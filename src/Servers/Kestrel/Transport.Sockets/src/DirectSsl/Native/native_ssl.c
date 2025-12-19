/*
 * Async TLS Native Layer
 * 
 * This module provides low-level SSL/TLS operations with epoll-based async I/O.
 * The managed (C#) layer handles:
 *   - Socket accept (await socket.AcceptAsync())
 *   - Application logic with decrypted data
 * 
 * The native layer handles:
 *   - Non-blocking SSL_do_handshake with epoll scheduling
 *   - epoll_wait for I/O readiness
 *   - SSL_read/SSL_write for encrypted communication 
 */

#include <openssl/ssl.h>
#include <openssl/err.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/epoll.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>

// ============================================================================
// Return codes for ssl_try_handshake()
// These are returned to C# to indicate the handshake state
// ============================================================================
#define HANDSHAKE_COMPLETE      0   // Handshake finished successfully
#define HANDSHAKE_WANT_READ     1   // Need to wait for socket readable
#define HANDSHAKE_WANT_WRITE    2   // Need to wait for socket writable  
#define HANDSHAKE_ERROR        -1   // Handshake failed

// ============================================================================
// Epoll instance management
// Each worker thread should have its own epoll instance
// ============================================================================

/**
 * Create a new epoll instance.
 * 
 * Returns: epoll file descriptor (>= 0) on success, -1 on error
 */
int create_epoll(void) {
    int epoll_fd = epoll_create1(0);
    if (epoll_fd < 0) {
        perror("[native] epoll_create1 failed");
    }
    return epoll_fd;
}

/**
 * Close an epoll instance.
 */
void close_epoll(int epoll_fd) {
    if (epoll_fd >= 0) {
        close(epoll_fd);
    }
}

// ============================================================================
// Socket utilities
// ============================================================================

/**
 * Set a socket to non-blocking mode.
 * 
 * When the socket is non-blocking:
 *   - read() returns EAGAIN if no data available (instead of blocking)
 *   - write() returns EAGAIN if buffer full (instead of blocking)
 *   - SSL_do_handshake() internally uses read()/write(), so it inherits this
 * 
 * Returns: 0 on success, -1 on error
 */
int set_socket_nonblocking(int fd) {
    int flags = fcntl(fd, F_GETFL, 0);
    if (flags == -1) {
        perror("[native] fcntl F_GETFL failed");
        return -1;
    }
    if (fcntl(fd, F_SETFL, flags | O_NONBLOCK) == -1) {
        perror("[native] fcntl F_SETFL O_NONBLOCK failed");
        return -1;
    }
    return 0;
}

/**
 * Set TCP_NODELAY on socket (disable Nagle's algorithm).
 * Important for low-latency handshakes.
 */
int set_tcp_nodelay(int fd) {
    int flag = 1;
    return setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, &flag, sizeof(flag));
}

// ============================================================================
// SSL Connection Management
// ============================================================================

/**
 * Create a new SSL object for a client connection AND register with epoll.
 * 
 * ADD to epoll here, then only MOD in ssl_try_handshake.
 * 
 * Parameters:
 *   ssl_ctx: The SSL_CTX created by C# (shared across connections)
 *   client_fd: The accepted socket file descriptor
 *   epoll_fd: The epoll instance to register with
 * 
 * Returns: SSL* pointer on success, NULL on error
 */
SSL* ssl_connection_create(SSL_CTX* ssl_ctx, int client_fd, int epoll_fd) {
    if (ssl_ctx == NULL) {
        fprintf(stderr, "[native] ssl_connection_create: ssl_ctx is NULL\n");
        return NULL;
    }
    
    // Make socket non-blocking BEFORE creating SSL
    // This ensures SSL_do_handshake won't block
    if (set_socket_nonblocking(client_fd) < 0) {
        return NULL;
    }
    
    // Optional: Set TCP_NODELAY for lower latency
    set_tcp_nodelay(client_fd);
    
    // Create new SSL object from context
    SSL* ssl = SSL_new(ssl_ctx);
    if (ssl == NULL) {
        fprintf(stderr, "[native] SSL_new failed\n");
        ERR_print_errors_fp(stderr);
        return NULL;
    }
    
    // Associate SSL with the socket file descriptor
    // This tells OpenSSL to use this FD for all read/write operations
    if (SSL_set_fd(ssl, client_fd) != 1) {
        fprintf(stderr, "[native] SSL_set_fd failed\n");
        ERR_print_errors_fp(stderr);
        SSL_free(ssl);
        return NULL;
    }
    
    // Set SSL to accept mode (server-side)
    // This configures the state machine for server handshake
    SSL_set_accept_state(ssl);
    
    // Register with epoll - EPOLLIN initially (waiting for ClientHello)
    // Like async_mt: ADD here once, then only MOD in ssl_try_handshake
    struct epoll_event ev;
    ev.events = EPOLLIN | EPOLLET;
    ev.data.fd = client_fd;
    if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, client_fd, &ev) < 0) {
        perror("[native] epoll_ctl ADD failed");
        SSL_free(ssl);
        return NULL;
    }
    
    return ssl;
}

/**
 * Destroy an SSL connection.
 */
void ssl_connection_destroy(SSL* ssl) {
    if (ssl != NULL) {
        SSL_shutdown(ssl);
        SSL_free(ssl);
    }
}

// ============================================================================
// Core Async Handshake API
// ============================================================================

/**
 * Try to advance the TLS handshake.
 * 
 * How it works:
 *   1. Calls SSL_do_handshake() which tries to progress the handshake
 *   2. If handshake completes → return HANDSHAKE_COMPLETE
 *   3. If OpenSSL needs to read (waiting for client data):
 *      - Register socket with epoll for EPOLLIN (readable)
 *      - Return HANDSHAKE_WANT_READ
 *   4. If OpenSSL needs to write (send buffer full):
 *      - Register socket with epoll for EPOLLOUT (writable)
 *      - Return HANDSHAKE_WANT_WRITE
 *   5. On error → return HANDSHAKE_ERROR
 * 
 * The C# layer then:
 *   - If WANT_READ/WRITE: calls epoll_wait_one() to wait
 *   - Then calls ssl_try_handshake() again
 *   - Repeats until COMPLETE or ERROR
 * 
 * Parameters:
 *   ssl: The SSL object
 *   client_fd: Socket FD (needed for epoll registration)
 *   epoll_fd: The epoll instance to register events with
 * 
 * Returns: HANDSHAKE_COMPLETE, HANDSHAKE_WANT_READ, HANDSHAKE_WANT_WRITE, or HANDSHAKE_ERROR
 */
int ssl_try_handshake(SSL* ssl, int client_fd, int epoll_fd) {
    int ret = SSL_do_handshake(ssl);
    
    if (ret == 1) {
        // Handshake is complete; remove from epoll if registered
        epoll_ctl(epoll_fd, EPOLL_CTL_DEL, client_fd, NULL);
        return HANDSHAKE_COMPLETE;
    }
    
    int err = SSL_get_error(ssl, ret);
    if (err == SSL_ERROR_WANT_READ || err == SSL_ERROR_WANT_WRITE) {
        
        // OpenSSL needs I/O - modify epoll registratio        
        struct epoll_event ev;
        ev.data.fd = client_fd;
        ev.events = (err == SSL_ERROR_WANT_READ) ? EPOLLIN : EPOLLOUT;
        ev.events |= EPOLLET;
        
        // Socket was already ADDed in ssl_connection_create, only MOD here
        epoll_ctl(epoll_fd, EPOLL_CTL_MOD, client_fd, &ev);
        
        return (err == SSL_ERROR_WANT_READ) ? HANDSHAKE_WANT_READ : HANDSHAKE_WANT_WRITE;
    }
    
    // Real error occurred
    if (err == SSL_ERROR_SYSCALL) {
        // Check errno for the actual error
        if (errno != 0) {
            perror("[native] SSL_do_handshake syscall error");
        }
    } else if (err == SSL_ERROR_SSL) {
        fprintf(stderr, "[native] SSL_do_handshake SSL error\n");
        ERR_print_errors_fp(stderr);
    }
    
    return HANDSHAKE_ERROR;
}

/**
 * Wait for an I/O event on the epoll instance.
 * This blocks until the socket is ready (readable or writable).
 * 
 * Parameters:
 *   epoll_fd: The epoll instance
 *   timeout_ms: Timeout in milliseconds (-1 for infinite)
 * 
 * Returns:
 *   > 0: Socket FD that is ready
 *   0: Timeout (no events)
 *   -1: Error
 */
int epoll_wait_one(int epoll_fd, int timeout_ms) {
    struct epoll_event event;
    
    int nfds = epoll_wait(epoll_fd, &event, 1, timeout_ms);
    
    if (nfds < 0) {
        if (errno == EINTR) {
            return 0; // Interrupted, treat as timeout
        }
        perror("[native] epoll_wait failed");
        return -1;
    }
    
    if (nfds == 0) {
        return 0; // Timeout
    }
    
    // Return the FD that is ready
    return event.data.fd;
}

// ============================================================================
// SSL Read/Write for Application Data
// ============================================================================

/**
 * Read decrypted data from the SSL connection.
 * 
 * After handshake completes, use this to read application data.
 * SSL_read() handles:
 *   1. Reading encrypted data from socket
 *   2. Decrypting it
 *   3. Returning plaintext to the caller
 * 
 * Parameters:
 *   ssl: The SSL object (handshake must be complete)
 *   buffer: Buffer to receive decrypted data
 *   buffer_size: Maximum bytes to read
 * 
 * Returns:
 *   > 0: Number of bytes read
 *   0: Connection closed (EOF)
 *   -1: Would block (WANT_READ) - no data available yet
 *   -2: Error
 */
int ssl_read(SSL* ssl, char* buffer, int buffer_size) {
    int ret = SSL_read(ssl, buffer, buffer_size);
    
    if (ret > 0) {
        return ret;  // Success, return bytes read
    }
    
    int err = SSL_get_error(ssl, ret);
    
    if (err == SSL_ERROR_WANT_READ) {
        return -1;  // Would block, try again later
    }
    
    if (err == SSL_ERROR_ZERO_RETURN) {
        return 0;  // Clean shutdown
    }
    
    // Error
    return -2;
}

/**
 * Write data through the SSL connection (encrypts and sends).
 * 
 * SSL_write() handles:
 *   1. Encrypting the plaintext
 *   2. Writing encrypted data to socket
 * 
 * Parameters:
 *   ssl: The SSL object (handshake must be complete)
 *   data: Plaintext data to send
 *   length: Number of bytes to send
 * 
 * Returns:
 *   > 0: Number of bytes written
 *   -1: Would block (WANT_WRITE) - buffer full
 *   -2: Error
 */
int ssl_write(SSL* ssl, const char* data, int length) {
    int ret = SSL_write(ssl, data, length);
    
    if (ret > 0) {
        return ret;  // Success, return bytes written
    }
    
    int err = SSL_get_error(ssl, ret);
    
    if (err == SSL_ERROR_WANT_WRITE) {
        return -1;  // Would block, try again later
    }
    
    // Error
    return -2;
}

/**
 * Get the file descriptor associated with an SSL connection.
 */
int ssl_get_fd(SSL* ssl) {
    return SSL_get_fd(ssl);
}

// ============================================================================
// Epoll Registration for I/O (post-handshake)
// ============================================================================

/**
 * Register socket for read events (EPOLLIN).
 * Used after SSL_read returns WANT_READ to wait for data.
 * 
 * This uses EPOLL_CTL_MOD since socket was already added during handshake.
 * If socket wasn't added, falls back to EPOLL_CTL_ADD.
 */
int epoll_register_read(int epoll_fd, int client_fd) {
    struct epoll_event ev;
    ev.events = EPOLLIN | EPOLLET;
    ev.data.fd = client_fd;
    
    // Try MOD first (socket already in epoll from handshake)
    if (epoll_ctl(epoll_fd, EPOLL_CTL_MOD, client_fd, &ev) < 0) {
        // If MOD fails, try ADD (socket not in epoll yet)
        if (errno == ENOENT) {
            if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, client_fd, &ev) < 0) {
                perror("[native] epoll_register_read ADD failed");
                return -1;
            }
        } else {
            perror("[native] epoll_register_read MOD failed");
            return -1;
        }
    }
    return 0;
}

/**
 * Register socket for write events (EPOLLOUT).
 * Used after SSL_write returns WANT_WRITE to wait for buffer space.
 */
int epoll_register_write(int epoll_fd, int client_fd) {
    struct epoll_event ev;
    ev.events = EPOLLOUT | EPOLLET;
    ev.data.fd = client_fd;
    
    // Try MOD first (socket already in epoll from handshake)
    if (epoll_ctl(epoll_fd, EPOLL_CTL_MOD, client_fd, &ev) < 0) {
        // If MOD fails, try ADD (socket not in epoll yet)
        if (errno == ENOENT) {
            if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, client_fd, &ev) < 0) {
                perror("[native] epoll_register_write ADD failed");
                return -1;
            }
        } else {
            perror("[native] epoll_register_write MOD failed");
            return -1;
        }
    }
    return 0;
}
