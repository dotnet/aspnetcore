# Data Protection and Identity (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## PBKDF2 delegates to platform one-shot API

NetCorePbkdf2Provider maps the configured PRF to HashAlgorithmName and calls Rfc2898DeriveBytes.Pbkdf2 in one operation.

- Do: Prefer Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithmName, outputLength) over custom PBKDF2 loops on modern targets.
- Why: Using the runtime's optimized PBKDF2 implementation avoids slower managed loops and repeated temporary allocations.
- Source: `DataProtection\Cryptography.KeyDerivation\src\PBKDF2\NetCorePbkdf2Provider.cs#L24-L40` (Cryptography.KeyDerivation PBKDF2)
- Hot path: yes | Complexity: low
- APIs: `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2`, `System.Security.Cryptography.HashAlgorithmName`

## Password hashes use fixed-time comparison

PasswordHasher verifies both V2 and V3 password hashes with CryptographicOperations.FixedTimeEquals on modern targets.

- Do: Use CryptographicOperations.FixedTimeEquals for secret or password-derived byte comparisons.
- Why: Hash verification avoids data-dependent early exits that could leak matching prefix information.
- Source: `Identity\Extensions.Core\src\PasswordHasher.cs#L220-L244` (Identity password hashing)
- Hot path: yes | Complexity: low
- APIs: `System.Security.Cryptography.CryptographicOperations.FixedTimeEquals`

## Span unprotect parses headers without copying

KeyRingBasedSpanDataProtector reads the magic header and key id from ReadOnlySpan<byte>, slices off the ciphertext, and decrypts into the destination writer.

- Do: Use BinaryPrimitives.ReadUInt32BigEndian, new Guid(ReadOnlySpan<byte>), slicing, and IBufferWriter-based decrypt.
- Why: Span parsing avoids allocating header arrays and lets decrypt stream plaintext directly to the caller buffer.
- Source: `DataProtection\DataProtection\src\KeyManagement\KeyRingBasedSpanDataProtector.cs#L90-L185` (DataProtection key ring unprotect)
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.IBufferWriter<T>`, `System.Buffers.Binary.BinaryPrimitives`, `System.Guid`

## AesGcm clears stackallocated key material

AesGcmAuthenticatedEncryptor stackallocs decrypted KDK and derived key for common sizes and clears both spans in a finally block after encrypt or decrypt.

- Do: Guard stackalloc sizes, pin spans only while writing secrets, and Clear all secret spans in finally.
- Why: Short-lived key material avoids heap allocation and is explicitly scrubbed regardless of cryptographic failures.
- Source: `DataProtection\DataProtection\src\Managed\AesGcmAuthenticatedEncryptor.cs#L240-L280` (Managed AES-GCM key derivation)
- Hot path: yes | Complexity: medium
- APIs: `System.Span<T>`, `stackalloc`, `Span<T>.Clear`

## AesGcm span encryption in caller buffer

AesGcmAuthenticatedEncryptor slices one destination span into key modifier, nonce, encrypted data, and tag, then calls AesGcm.Encrypt with span destinations.

- Do: Precompute total payload size, get a destination span, slice it, and call AesGcm.Encrypt(nonce, plaintext, encrypted, tag).
- Why: GCM ciphertext and tag are produced directly in the final payload layout without staging arrays.
- Source: `DataProtection\DataProtection\src\Managed\AesGcmAuthenticatedEncryptor.cs#L216-L273` (Managed AES-GCM encryptor)
- Hot path: yes | Complexity: medium
- APIs: `System.Security.Cryptography.AesGcm`, `System.Buffers.IBufferWriter<T>`, `Span<T>.Slice`

## Bounded stackalloc for crypto subkeys

ManagedAuthenticatedEncryptor stackallocs decrypted KDK, encryption, and validation subkeys for normal key sizes and falls back to arrays for larger sizes.

- Do: Use length <= 128 or <= 256 guarded stackalloc slices with array fallback for crypto working buffers.
- Why: Small secret buffers stay off the managed heap while size guards prevent unbounded stack usage.
- Source: `DataProtection\DataProtection\src\Managed\ManagedAuthenticatedEncryptor.cs#L187-L227` (Managed CBC plus HMAC encryptor)
- Hot path: yes | Complexity: medium
- APIs: `System.Span<T>`, `stackalloc`

## CNG CBC constant-time HMAC validation

CbcAuthenticatedEncryptor computes the actual HMAC digest into stack memory and compares it to the payload digest with TimeConstantBuffersAreEqual.

- Do: Stackalloc the digest-sized buffer, hash into it, and use a fixed-time byte comparison.
- Why: The validation path avoids heap allocation for the digest and avoids timing leaks while checking integrity.
- Source: `DataProtection\DataProtection\src\Cng\CbcAuthenticatedEncryptor.cs#L466-L472` (CNG CBC plus HMAC validation)
- Hot path: yes | Complexity: medium
- APIs: `stackalloc`, `BCryptHashData`, `CryptoUtil.TimeConstantBuffersAreEqual`

## CNG CBC size query before destination allocation

CbcAuthenticatedEncryptor calls BCryptEncrypt with null output to get padded ciphertext size, then obtains exactly one destination span for the final payload.

- Do: Use the native size-query call before destination.GetSpan(totalRequiredSize), then encrypt directly to the payload slice.
- Why: The encrypt path allocates only the required destination size while still supporting block padding output expansion.
- Source: `DataProtection\DataProtection\src\Cng\CbcAuthenticatedEncryptor.cs#L290-L360` (CNG CBC plus HMAC encryptor)
- Hot path: yes | Complexity: medium
- APIs: `BCryptEncrypt`, `System.Buffers.IBufferWriter<T>`

## Destination-sized CBC encryption

ManagedAuthenticatedEncryptor computes the CBC ciphertext length up front, gets one destination span, encrypts into the ciphertext slice, and writes the MAC into the final slice.

- Do: Call SymmetricAlgorithm.GetCiphertextLengthCbc, GetSpan(totalSize), EncryptCbc(source, iv, destination), and span HMAC APIs.
- Why: One output buffer with fixed slices avoids MemoryStream, CryptoStream, and intermediate ciphertext or MAC arrays on modern targets.
- Source: `DataProtection\DataProtection\src\Managed\ManagedAuthenticatedEncryptor.cs#L177-L261` (Managed CBC plus HMAC encryptor)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.IBufferWriter<T>`, `SymmetricAlgorithm.GetCiphertextLengthCbc`, `SymmetricAlgorithm.EncryptCbc`, `HMACSHA256.HashData`, `HMACSHA512.HashData`

## PBKDF2 password encoding uses guarded stackalloc and zeroing

Win8Pbkdf2Provider encodes the password into a stack buffer when GetMaxByteCount is under the maximum stackalloc threshold, otherwise uses a heap array, and securely zeros the buffer.

- Do: Check byte count against Constants.MAX_STACKALLOC_BYTES before stackalloc and call SecureZeroMemory in finally.
- Why: Typical passwords avoid heap allocation while long passwords cannot overflow the stack, and plaintext password bytes are scrubbed.
- Source: `DataProtection\Cryptography.KeyDerivation\src\PBKDF2\Win8Pbkdf2Provider.cs#L62-L94` (Windows PBKDF2 provider)
- Hot path: yes | Complexity: medium
- APIs: `stackalloc`, `Encoding.UTF8.GetBytes`, `UnsafeBufferUtil.SecureZeroMemory`

## SP800-108 span KDF fills caller-provided subkeys

ManagedSP800_108_CTR_HMACSHA512 copies HMAC output directly into operationSubkey and validationSubkey spans and clears PRF output each iteration.

- Do: Accept Span<byte> subkey destinations, copy slices from PRF output into them, and Clear temporary PRF buffers.
- Why: Caller-provided output spans avoid allocating derived-key arrays and explicit clearing limits exposure of key material.
- Source: `DataProtection\DataProtection\src\SP800_108\ManagedSP800_108_CTR_HMACSHA512.cs#L146-L175` (SP800-108 key derivation)
- Hot path: yes | Complexity: medium
- APIs: `System.Span<T>`, `Span<T>.CopyTo`, `Span<T>.Clear`

## SP800-108 stackalloc input and output buffers

ManagedSP800_108_CTR_HMACSHA512 stackallocs PRF output and PRF input when sizes are at most 128 bytes, otherwise falling back to arrays.

- Do: Build PRF input in a reusable Span<byte>, mutate only the counter bytes per iteration, and use stackalloc only under a small threshold.
- Why: Per-iteration key derivation avoids heap churn for the common compact label/context sizes while still bounding stack usage.
- Source: `DataProtection\DataProtection\src\SP800_108\ManagedSP800_108_CTR_HMACSHA512.cs#L89-L141` (SP800-108 key derivation)
- Hot path: yes | Complexity: medium
- APIs: `System.Span<T>`, `stackalloc`, `HMACSHA512.TryHashData`

## Span encryptor writes directly to IBufferWriter

KeyRingBasedSpanDataProtector emits the magic header and key id into the caller's IBufferWriter and then asks the span encryptor to append ciphertext.

- Do: Implement span overloads that take ReadOnlySpan<byte> input and ref TWriter where TWriter : IBufferWriter<byte>.
- Why: This avoids building an intermediate byte array for protect and unprotect hot paths when callers can provide a destination buffer.
- Source: `DataProtection\DataProtection\src\KeyManagement\KeyRingBasedSpanDataProtector.cs#L25-L79` (DataProtection key ring protector)
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.IBufferWriter<T>`, `System.Buffers.Binary.BinaryPrimitives`, `Guid.TryWriteBytes`

## Span hash validation with constant-time compare

ManagedAuthenticatedEncryptor computes the expected MAC into a stack or heap Span<byte>, compares with CryptoUtil.TimeConstantBuffersAreEqual, then clears the hash buffer.

- Do: Use HMACSHA256.HashData or HMACSHA512.HashData into a destination span, then time-constant compare and Clear the temporary span.
- Why: The code avoids per-call hash arrays for common HMACs and preserves side-channel resistant comparison and cleanup.
- Source: `DataProtection\DataProtection\src\Managed\ManagedAuthenticatedEncryptor.cs#L394-L429` (Managed CBC plus HMAC MAC validation)
- Hot path: yes | Complexity: medium
- APIs: `HMACSHA256.HashData`, `HMACSHA512.HashData`, `HashAlgorithm.TryComputeHash`, `Span<T>.Clear`

## CNG CBC derives combined subkey buffer

CbcAuthenticatedEncryptor stackallocs one contiguous temp buffer for encryption and HMAC subkeys, then slices it with pointer offsets.

- Do: Compute combined key length, stackalloc once, split with offsets, and SecureZeroMemory the whole region in finally.
- Why: A single stack allocation reduces heap pressure and groups sensitive material for one secure zeroing operation.
- Source: `DataProtection\DataProtection\src\Cng\CbcAuthenticatedEncryptor.cs#L252-L369` (CNG CBC plus HMAC encryptor)
- Hot path: yes | Complexity: high
- APIs: `stackalloc`, `UnsafeBufferUtil.SecureZeroMemory`, `BCryptGenerateSymmetricKey`

## CNG GCM decrypts directly into IBufferWriter

CngGcmAuthenticatedEncryptor parses ciphertext with pointers, derives a stackallocated decryption subkey, gets a plaintext span, and passes it as BCryptDecrypt output.

- Do: After validating minimum payload size, derive into stackalloc, call destination.GetSpan(plaintextLength), and advance by BCryptDecrypt's byte count.
- Why: The authenticated decrypt path avoids plaintext staging arrays and scrubs native key material after use.
- Source: `DataProtection\DataProtection\src\Cng\CngGcmAuthenticatedEncryptor.cs#L58-L138` (CNG AES-GCM decryptor)
- Hot path: yes | Complexity: high
- APIs: `System.Buffers.IBufferWriter<T>`, `BCryptDecrypt`, `UnsafeBufferUtil.SecureZeroMemory`

## CNG GCM stackallocs native subkeys and writes output in place

CngGcmAuthenticatedEncryptor derives the symmetric subkey into stack memory, writes key modifier and nonce into the output buffer, and calls BCryptEncrypt into the ciphertext and tag slices.

- Do: Use stackalloc for native key buffers, generate nonce/key modifier directly in the output span, and zero key buffers with SecureZeroMemory.
- Why: Native crypto interop avoids managed intermediate arrays and keeps temporary key material off the heap.
- Source: `DataProtection\DataProtection\src\Cng\CngGcmAuthenticatedEncryptor.cs#L218-L283` (CNG AES-GCM encryptor)
- Hot path: yes | Complexity: high
- APIs: `System.Buffers.IBufferWriter<T>`, `stackalloc`, `BCryptEncrypt`, `UnsafeBufferUtil.SecureZeroMemory`

## Base32 generation with string.Create and stackalloc randomness

Base32.GenerateBase32 creates the exact output string and fills its buffer while random bytes live in a fixed-size stack span.

- Do: Use string.Create for known output length and stackalloc fixed-size random input inside the fill callback.
- Why: The authenticator secret path avoids a random byte array and StringBuilder for the generated Base32 string.
- Source: `Identity\Extensions.Core\src\Base32.cs#L17-L48` (Identity authenticator key generation)
- Hot path: either | Complexity: low
- APIs: `string.Create`, `stackalloc`, `RandomNumberGenerator.Fill`

## Passkey base64url converter uses stackalloc then ArrayPool

BufferSourceJsonConverter decodes and encodes base64url with Utf8 span APIs, using a 256-byte stack threshold and ArrayPool<byte> for larger payloads.

- Do: Use Base64Url.DecodeFromUtf8 and EncodeToUtf8 over spans with stackalloc for small buffers and ArrayPool fallback.
- Why: Most WebAuthn buffers avoid heap scratch arrays while larger buffers reuse pooled memory during JSON conversion.
- Source: `Identity\Core\src\Passkeys\BufferSourceJsonConverter.cs#L43-L95` (Identity passkey JSON serialization)
- Hot path: either | Complexity: low
- APIs: `System.Buffers.Text.Base64Url`, `System.Buffers.ArrayPool<T>`, `Utf8JsonWriter.WriteStringValue`, `stackalloc`

## Passkey buffers expose spans and span equality

BufferSource stores ReadOnlyMemory<byte>, exposes AsSpan for consumers, and uses Span.SequenceEqual for value equality.

- Do: Store byte payloads as ReadOnlyMemory<byte>, expose AsSpan, and compare with ReadOnlySpan<byte>.SequenceEqual.
- Why: Passkey code can inspect bytes without ToArray allocations and equality uses optimized span comparison.
- Source: `Identity\Core\src\Passkeys\BufferSource.cs#L22-L78` (Identity passkey buffer model)
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlyMemory<T>`, `System.ReadOnlySpan<T>`, `MemoryExtensions.SequenceEqual`

## TOTP one-shot HMAC into stack spans

Rfc6238AuthenticationService writes the timestep into an 8-byte stack span and hashes it with HMACSHA1.TryHashData into a stackallocated hash buffer.

- Do: Use BitConverter.TryWriteBytes with a stack span and HMACSHA1.TryHashData(key, data, destination, out written).
- Why: Token generation and validation avoid BitConverter and ComputeHash arrays on modern targets.
- Source: `Identity\Extensions.Core\src\Rfc6238AuthenticationService.cs#L37-L55` (Identity TOTP token provider)
- Hot path: either | Complexity: low
- APIs: `stackalloc`, `BitConverter.TryWriteBytes`, `HMACSHA1.TryHashData`

## ArrayPool fallback for byte[] compatibility APIs

ManagedAuthenticatedEncryptor's byte[] Encrypt and Decrypt overloads use a stack buffer for outputs up to 256 bytes and ArrayPool<byte> for larger outputs before copying the written span to the return array.

- Do: Use stackalloc for small outputSize, ArrayPool<byte>.Shared.Rent for larger output, RefPooledArrayBufferWriter, and Return(clearArray: true).
- Why: Legacy byte[] APIs still reduce transient allocations while guaranteeing pooled buffers containing secrets are cleared on return.
- Source: `DataProtection\DataProtection\src\Managed\ManagedAuthenticatedEncryptor.cs#L286-L310` (Managed DataProtection byte array adapter)
- Hot path: either | Complexity: medium
- APIs: `System.Buffers.ArrayPool<T>`, `RefPooledArrayBufferWriter<T>`, `stackalloc`

## Passkey parsing slices ReadOnlyMemory instead of copying

AuthenticatorData.ParseCore walks an offset through ReadOnlyMemory<byte>, slices RpIdHash and remaining data, and reads SignCount with BinaryPrimitives.

- Do: Track offsets, use ReadOnlyMemory<byte>.Slice and Span for fixed-width reads, and only parse nested data from remaining slices.
- Why: Parsing authenticator data keeps references to the original payload and avoids allocating subarrays for fixed fields.
- Source: `Identity\Core\src\Passkeys\AuthenticatorData.cs#L91-L145` (Identity passkey authenticator data parser)
- Hot path: either | Complexity: medium
- APIs: `System.ReadOnlyMemory<T>`, `System.Buffers.Binary.BinaryPrimitives`, `System.Formats.Cbor.CborReader`
