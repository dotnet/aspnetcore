// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static class Constants
{
    public const string Password = "password";

    public const string Key =
      // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Dummy certificate for testing.")]
      @"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
        AgIH0A==";

    public const string KeyRingXmlFileName = "key-9c15d488-4417-49ab-ae39-ed2f36a9ebe3.xml";
    public const string KeyRingXmlContents = """
        <?xml version="1.0" encoding="utf-8"?>
        <key id="9c15d488-4417-49ab-ae39-ed2f36a9ebe3" version="1">
          <creationDate>2023-05-04T19:16:30.3590154Z</creationDate>
          <activationDate>2023-05-04T19:16:30.3487875Z</activationDate>
          <expirationDate>2115-08-02T19:16:30.3487875Z</expirationDate>
          <descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection">
            <descriptor>
              <encryption algorithm="AES_256_CBC" />
              <validation algorithm="HMACSHA256" />
              <encryptedSecret decryptorType="Microsoft.AspNetCore.DataProtection.XmlEncryption.EncryptedXmlDecryptor, Microsoft.AspNetCore.DataProtection" xmlns="http://schemas.asp.net/2015/03/dataProtection">
                <EncryptedData Type="http://www.w3.org/2001/04/xmlenc#Element" xmlns="http://www.w3.org/2001/04/xmlenc#">
                  <EncryptionMethod Algorithm="http://www.w3.org/2001/04/xmlenc#aes256-cbc" />
                  <KeyInfo xmlns="http://www.w3.org/2000/09/xmldsig#">
                    <EncryptedKey xmlns="http://www.w3.org/2001/04/xmlenc#">
                      <EncryptionMethod Algorithm="http://www.w3.org/2001/04/xmlenc#rsa-1_5" />
                      <KeyInfo xmlns="http://www.w3.org/2000/09/xmldsig#">
                        <X509Data>
                          <X509Certificate>MIIDATCCAemgAwIBAgIQNH2CfRDvN5FK9yzBy/U8bTANBgkqhkiG9w0BAQsFADAbMRkwFwYDVQQDDBB1c2VyQGV4YW1wbGUuY29tMCAXDTE1MTAwNjA4NDQ0OFoYDzIxMTUxMDA2MDg0NDQ4WjAbMRkwFwYDVQQDDBB1c2VyQGV4YW1wbGUuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvjSzpvCfj6B3MGJWDjUc+LlxZAF8Szz+tTiX9inLIdiEG1LfpJlHgrNXumnY8ZRph1BOMH8/XS28tlhz1iLSX/3EYNLwWhi/9YAT6mC+qmCJu4I+DAN0Xf/lDbRyhB/6iYgPxtuDA3yVGcrh1NQY5JibxGF7uPN8+SmMAff0rP3eseacroYsiis6SG7ItBg+2M78ZJMuPTwl+abiOSwBTtXog8/v87yk6U/VyGzaesygvHCS39tEBT8Uw6AjPEABQI3Rr95LpdbqguzQh/xzwdCp1YFlKiUHkavspg2PiI7qMIamI8H65rXh4emwlurTbTNq8anIbKuYUDbp6XEUeQIDAQABoz8wPTAOBgNVHQ8BAf8EBAMCBLAwDAYDVR0TAQH/BAIwADAdBgNVHQ4EFgQUzx66AAJJ4/RjAJ5dqkeTGHUATFYwDQYJKoZIhvcNAQELBQADggEBADe2e7NytbwtvbuPxX+zm2xOfzf+f335DPxll3MFNZCKM7XThkZmZn49QLh2ltmRR6e/+FiG9nWILADXJvKJFOBl/Nm+ektVXGyCk6ftcAWJnQHgY96ih0dvtn+cTIdr9iYlgICyXDJKvexN15TypbXwsKe9jnuOYvDOn2FtEKi8ysnaHEkVimCMb2DkZZKNm2yQAW5DRxfDZGTsGqeB3R3Li/AQR0DSAOFHjKEHwqolay8Y+5+3XVFi/tK8opVMO39BcP7Ag56IfC20MpzNI8Ibc7OrSy0SMG7QnYd7XT483AAiaKQnhTzmEQwhlILxOiNyi6PGbKH5fshmHRRq1Q8=</X509Certificate>
                        </X509Data>
                      </KeyInfo>
                      <CipherData>
                        <CipherValue>ahdd65gng0TzxZ/6Nw1zLvqFi3lsvz4tvAF4pBz7BZCqTHbTzaRnK5rMWSP9wEk8fa0LN9CGw7y1QxKfuPneTooYogDPGdVc6T8IbmASGfQI1SZZy7yOkF4IcTz9txJ4wbcjf4hUXPvYeBb0l1QlvKLh0BpXquhZqeM8Gat4IXz1iQbTCB31regpmeDSGJz6ok1ZFhjn0v4K5YvXucofxTmh+0aONveMWyadp56dP4KJVM5x4z6UddiFdVlk4euuSX6qbVHI7eA4UGSdnu8lsbBa5aiypS0kRO8NGyab+nSY907hn8E7mUCc9MjyS/QwFSZyT3y88q0pb1bK8zL8tA==</CipherValue>
                      </CipherData>
                    </EncryptedKey>
                  </KeyInfo>
                  <CipherData>
                    <CipherValue>YTcCujeLvS47fgVUWLUC9FaZJDp+MSMtuxAkUWfAh5xnbKI8i0dFulIftz+lykfx+/1dMmQ/GsFSbbqhjCG6GDUKz5/eBXEg/It9Vagu9y0UTlrfj0dLaSG7jaw+wNY9DXTsvRtIHkEI0XQHWr0HqkJYVDfiA4yRZu2Sfue9nSmIZFMhCZOP286hJ0JFsqX1+phX3sR6JX31mGKopltlAsIGgL8MRTXWp5tZxuk9W+sgoWfJziDYmrPP2P6eMn7AdX4T+TDbuAY7MtaExL8Itv8WeKN34lxXocT7hRIjHaZMfiqocDX6YcIjnYhgeKQ1XlFo4j4fZ/p4PwxMmXdhrr/yAhzPhhOt6kqncsNnfVWbPS6+dh+3XE6/K0Oa2B5avbYKOv21KU69mSaXc4EsPA==</CipherValue>
                  </CipherData>
                </EncryptedData>
              </encryptedSecret>
            </descriptor>
          </descriptor>
        </key>
        """;
}
