#ifndef NATIVE_SSL_H
#define NATIVE_SSL_H

#include <openssl/ssl.h>

// ============================================================================
// Return codes for ssl_try_handshake()
// ============================================================================
#define HANDSHAKE_COMPLETE      0   // Handshake finished successfully
#define HANDSHAKE_WANT_READ     1   // Need to wait for socket readable
#define HANDSHAKE_WANT_WRITE    2   // Need to wait for socket writable  
#define HANDSHAKE_ERROR        -1   // Handshake failed

// ============================================================================
// Epoll Management
// ============================================================================

// Create a new epoll instance, returns epoll_fd or -1 on error
int create_epoll(void);

// Close an epoll instance
void close_epoll(int epoll_fd);

// Wait for one event, returns ready FD, 0 on timeout, -1 on error
int epoll_wait_one(int epoll_fd, int timeout_ms);

// Remove fd from epoll (call when connection closes)
int epoll_remove(int epoll_fd, int client_fd);

// ============================================================================
// Socket Utilities
// ============================================================================
// Set socket to non-blocking mode
int set_socket_nonblocking(int fd);

// Set TCP_NODELAY on socket
int set_tcp_nodelay(int fd);

// ============================================================================
// SSL Connection Management
// ============================================================================

// Create SSL object for a client connection AND register with epoll
// Makes socket non-blocking, associates with SSL_CTX, registers EPOLLIN
SSL* ssl_connection_create(SSL_CTX* ssl_ctx, int client_fd, int epoll_fd);

// Destroy SSL connection
void ssl_connection_destroy(SSL* ssl);

// Get the socket FD from SSL object
int ssl_get_fd(SSL* ssl);

// ============================================================================
// Core Async Handshake API
// ============================================================================

// Try to advance handshake, returns HANDSHAKE_* status
// If WANT_READ/WRITE, registers with epoll automatically
int ssl_try_handshake(SSL* ssl, int client_fd, int epoll_fd);

// Wait for epoll event, returns fd and event flags
// Returns: 1 = event, 0 = timeout, -1 = error
int epoll_wait_one_ex(int epoll_fd, int timeout_ms, int* out_fd, int* out_events);

// Epoll event flags for C# interop
#define NATIVE_EPOLLIN    0x001
#define NATIVE_EPOLLOUT   0x004
#define NATIVE_EPOLLERR   0x008
#define NATIVE_EPOLLHUP   0x010

// ============================================================================
// SSL Read/Write
// ============================================================================

// Read decrypted data, returns bytes read, 0 on EOF, -1 would block, -2 error
int ssl_read(SSL* ssl, char* buffer, int buffer_size);

// Write and encrypt data, returns bytes written, -1 would block, -2 error
int ssl_write(SSL* ssl, const char* data, int length);

// ============================================================================
// Error Handling
// ============================================================================

// Get the last OpenSSL error message. Returns number of bytes written to buffer.
// The buffer should be at least 256 bytes.
int ssl_get_last_error(char* buffer, int buffer_size);

#endif