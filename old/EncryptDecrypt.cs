/***************************************************************************
EncryptDecrypt.cs  - модуль Расшифровки строки, зашифрованной в PHP 
-------------------
begin                : 07 04 2021
copyright            : (C) 2021 by Гаммер Максим Дмитриевич (maximum2000)
email                : MaxGammer@gmail.com
site				 : lcontent.ru 
org					 : Гаммер Максим Дмитриевич
***************************************************************************/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using System.Collections.Generic;

//создано на основе
//https://stackoverflow.com/questions/19719294/decrypt-string-in-c-sharp-that-was-encrypted-with-php-openssl-encrypt
/*
$password = 'Ty63rs4aVqcnh2vUqRJTbNT26caRZJ';
$method = 'AES-256-CBC';
texteACrypter = 'Whether you think you can, or you think you can\'t--you\'re right. - Henry Ford';
$encrypted = openssl_encrypt($texteACrypter, $method, $password);
*/

public static class EncryptDecrypt
{
    public static string OpenSSLDecrypt(string encrypted, string passphrase)
    {
        //get the key bytes (not sure if UTF8 or ASCII should be used here doesn't matter if no extended chars in passphrase)
        var key = Encoding.UTF8.GetBytes(passphrase);

        //pad key out to 32 bytes (256bits) if its too short
        if (key.Length < 32)
        {
            var paddedkey = new byte[32];
            Buffer.BlockCopy(key, 0, paddedkey, 0, key.Length);
            key = paddedkey;
        }

        //setup an empty iv
        var iv = new byte[16];

        //get the encrypted data and decrypt
        byte[] encryptedBytes = Convert.FromBase64String(encrypted);
        return DecryptStringFromBytesAes(encryptedBytes, key, iv);
    }

    private static void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
    {
        // generate key and iv
        List<byte> concatenatedHashes = new List<byte>(48);

        byte[] password = Encoding.UTF8.GetBytes(passphrase);
        byte[] currentHash = new byte[0];
        MD5 md5 = MD5.Create();
        bool enoughBytesForKey = false;
        // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
        while (!enoughBytesForKey)
        {
            int preHashLength = currentHash.Length + password.Length + salt.Length;
            byte[] preHash = new byte[preHashLength];

            Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
            Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
            Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

            currentHash = md5.ComputeHash(preHash);
            concatenatedHashes.AddRange(currentHash);

            if (concatenatedHashes.Count >= 48)
                enoughBytesForKey = true;
        }

        key = new byte[32];
        iv = new byte[16];
        concatenatedHashes.CopyTo(0, key, 0, 32);
        concatenatedHashes.CopyTo(32, iv, 0, 16);

        md5.Clear();
    }

    static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
    {
        // Check arguments.
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (key == null || key.Length <= 0)
            throw new ArgumentNullException("key");
        if (iv == null || iv.Length <= 0)
            throw new ArgumentNullException("iv");

        // Declare the RijndaelManaged object
        // used to decrypt the data.
        RijndaelManaged aesAlg = null;

        // Declare the string used to hold
        // the decrypted text.
        string plaintext;

        // Create a RijndaelManaged object
        // with the specified key and IV.
        aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

        // Create a decrytor to perform the stream transform.
        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        // Create the streams used for decryption.
        using (MemoryStream msDecrypt = new MemoryStream(cipherText))
        {
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    // Read the decrypted bytes from the decrypting stream
                    // and place them in a string.
                    plaintext = srDecrypt.ReadToEnd();
                    srDecrypt.Close();
                }
            }
        }

        return plaintext;
    }

}