﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harrison314.EntityFrameworkCore.Encryption.CryptoProviders
{
    public sealed class PasswordDbContextEncryptedCryptoProvider : IDbContextEncryptedCryptoProvider, IDisposable
    {
        private readonly byte[] passwordData;
        const string PasswordName = "MasterPassword";

        public string ProviderName
        {
            get => "Password_v1";
        }

        public PasswordDbContextEncryptedCryptoProvider(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            this.passwordData = System.Text.Encoding.ASCII.GetBytes(password);
        }

        public ValueTask<MasterKeyData> EncryptMasterKey(byte[] masterKey, CancellationToken cancellationToken)
        {
            if (masterKey == null) throw new ArgumentNullException(nameof(masterKey));

            PasswordData passwordData = new PasswordData();
            passwordData.Iterations = 10000;
            passwordData.PasswordSalt = new byte[16];
            passwordData.AesGcmNonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            passwordData.AesGcmTag = new byte[AesGcm.TagByteSizes.MaxSize];

            RandomNumberGenerator.Fill(passwordData.PasswordSalt);
            RandomNumberGenerator.Fill(passwordData.AesGcmNonce);

            byte[] key = this.DerieveKey(passwordData);

            byte[] encryptedKey = new byte[masterKey.Length];

            using AesGcm aes = new AesGcm(key);
            aes.Encrypt(passwordData.AesGcmNonce, masterKey, encryptedKey, passwordData.AesGcmTag);

            MasterKeyData masterKeyData = new MasterKeyData()
            {
                Data = encryptedKey,
                KeyId = PasswordName,
                Parameters = System.Text.Json.JsonSerializer.Serialize(passwordData)
            };

            return new ValueTask<MasterKeyData>(masterKeyData);
        }

        public ValueTask<byte[]> DecryptMasterKey(MasterKeyData masterKeyData, CancellationToken cancellationToken)
        {
            if (masterKeyData == null) throw new ArgumentNullException(nameof(masterKeyData));

            PasswordData passwordData = System.Text.Json.JsonSerializer.Deserialize<PasswordData>(masterKeyData.Parameters);
            byte[] key = this.DerieveKey(passwordData);

            byte[] decryptedKey = new byte[masterKeyData.Data.Length];
            using AesGcm aes = new AesGcm(key);
            aes.Decrypt(passwordData.AesGcmNonce, masterKeyData.Data, passwordData.AesGcmTag, decryptedKey);

            return new ValueTask<byte[]>(decryptedKey);
        }

        private byte[] DerieveKey(PasswordData passwordData, int keySize = 32)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(this.passwordData, passwordData.PasswordSalt, passwordData.Iterations);
            return pbkdf2.GetBytes(keySize);
        }

        public ValueTask<string> FilterAcceptKeyIds(List<string> keyIds, CancellationToken cancellationToken)
        {
            if (keyIds == null) throw new ArgumentNullException(nameof(keyIds));

            if (keyIds.Contains(PasswordName))
            {
                return new ValueTask<string>(PasswordName);
            }
            else
            {
                throw new EfEncryptionException("Not found keyId in PasswordDbContextEncryptedCryptoProvider.");
            }
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(this.passwordData);
        }
    }
}