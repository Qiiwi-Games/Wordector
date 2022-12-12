using System.Security.Cryptography;
using System.IO;
using System;
using System.IO.Compression;

namespace Wordector
{
	public class Encryption
	{
		public static string Encrypt(byte[] data, byte[] key, byte[] salt)
		{
			byte[] encryptedData;

			using (SymmetricAlgorithm aes = SymmetricAlgorithm.Create())
			{
				aes.Mode = CipherMode.CBC;
				aes.Key = key;
				aes.IV = salt;
				aes.Padding = PaddingMode.PKCS7;
				ICryptoTransform encryptor = aes.CreateEncryptor();
				using (MemoryStream mStream = new MemoryStream())
				{
					using (CryptoStream cStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
					{
						cStream.Write(data, 0, data.Length);
						cStream.FlushFinalBlock();
						encryptedData = mStream.ToArray();
					}
				}
			}

			var encryptedString = Convert.ToBase64String(encryptedData);

			return encryptedString;
		}

		public static string EncryptString(byte[] plainBytes, byte[] key, byte[] iv)
		{
			SHA256 mySHA256 = SHA256Managed.Create();
			key = mySHA256.ComputeHash(key);

			// Instantiate a new Aes object to perform string symmetric encryption
			Aes encryptor = Aes.Create();

			encryptor.Mode = CipherMode.CBC;

			// Set key and IV
			byte[] aesKey = new byte[32];
			Array.Copy(key, 0, aesKey, 0, 32);
			encryptor.Key = aesKey;
			encryptor.IV = iv;

			// Instantiate a new MemoryStream object to contain the encrypted bytes
			MemoryStream memoryStream = new MemoryStream();

			// Instantiate a new encryptor from our Aes object
			ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

			// Instantiate a new CryptoStream object to process the data and write it to the 
			// memory stream
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

			// Encrypt the input plaintext string
			cryptoStream.Write(plainBytes, 0, plainBytes.Length);

			// Complete the encryption process
			cryptoStream.FlushFinalBlock();

			// Convert the encrypted data from a MemoryStream to a byte array
			byte[] cipherBytes = memoryStream.ToArray();

			// Close both the MemoryStream and the CryptoStream
			memoryStream.Close();
			cryptoStream.Close();

			// Convert the encrypted byte array to a base64 encoded string
			string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

			// Return the encrypted data as a string
			return cipherText;
		}

		public static byte[] Decrypt(byte[] data, byte[] key, byte[] salt)
		{
			byte[] decyptedData;

			using (SymmetricAlgorithm aes = SymmetricAlgorithm.Create())
			{
				aes.Mode = CipherMode.CBC;
				aes.Key = key;
				aes.IV = salt;
				aes.Padding = PaddingMode.PKCS7;

				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				using (MemoryStream msDecrypt = new MemoryStream(data))
				{
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						decyptedData = ReadFully(csDecrypt);
					}
				}
			}

			return decyptedData;
		}

		public static byte[] DecryptString(string cipherText, byte[] key, byte[] iv)
		{
			SHA256 mySHA256 = SHA256Managed.Create();
			key = mySHA256.ComputeHash(key);

			// Instantiate a new Aes object to perform string symmetric encryption
			Aes encryptor = Aes.Create();

			encryptor.Mode = CipherMode.CBC;

			// Set key and IV
			byte[] aesKey = new byte[32];
			Array.Copy(key, 0, aesKey, 0, 32);
			encryptor.Key = aesKey;
			encryptor.IV = iv;

			// Instantiate a new MemoryStream object to contain the encrypted bytes
			MemoryStream memoryStream = new MemoryStream();

			// Instantiate a new encryptor from our Aes object
			ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

			// Instantiate a new CryptoStream object to process the data and write it to the 
			// memory stream
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

			// Will contain decrypted plaintext
			byte[] plainBytes;

			try
			{
				// Convert the ciphertext string into a byte array
				byte[] cipherBytes = Convert.FromBase64String(cipherText);

				// Decrypt the input ciphertext string
				cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

				// Complete the decryption process
				cryptoStream.FlushFinalBlock();

				// Convert the decrypted data from a MemoryStream to a byte array
				plainBytes = memoryStream.ToArray();
			}
			finally
			{
				// Close both the MemoryStream and the CryptoStream
				memoryStream.Close();
				cryptoStream.Close();
			}

			// Return the decrypted data as a string
			return plainBytes;
		}

		public static byte[] ReadFully(Stream input)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}

		public static string sha1(string s)
		{
			SHA1 sha = new SHA1CryptoServiceProvider();
			return Encryption.ByteArrayToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)));
		}

		public static string sha1WithSalt(string s, byte[] salt)
		{
			Rfc2898DeriveBytes kf = new Rfc2898DeriveBytes(s, salt, 65536);
			byte[] key = kf.GetBytes(128 / 8);
			return ByteArrayToHexString(key);
		}

		public static string ByteArrayToHexString(byte[] ba)
		{
			string hex = BitConverter.ToString(ba);
			return hex.Replace("-", "");
		}

		public static byte[] SecuredRandom()
		{
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			byte[] random = new byte[16];
			rng.GetBytes(random);
			return random;
		}

		public static byte[] StringToByteArray(String hex)
		{
			int NumberChars = hex.Length / 2;
			byte[] bytes = new byte[NumberChars];
			using (var sr = new StringReader(hex))
			{
				for (int i = 0; i < NumberChars; i++)
					bytes[i] =
						Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
			}
			return bytes;
		}

		public static byte[] CompressWithGzip(byte[] data)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
				{
					gzip.Write(data, 0, data.Length);
				}
				return memory.ToArray();
			}
		}

		public static string EncryptAndGzipData(string data, byte[] salt)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
			string encryptedJsonBytes = Encryption.EncryptString(bytes, System.Text.Encoding.ASCII.GetBytes("C2BD366267E6DF64A0A73EBF74906E360F614166A7B6BDAB571DFEEA5EBF3C48"), salt);

			bytes = Encryption.CompressWithGzip(System.Text.Encoding.UTF8.GetBytes(encryptedJsonBytes));

			return Convert.ToBase64String(bytes);
			//----
			/*
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);

			bytes = Encryption.CompressWithGzip(bytes);

			string encryptedJsonBytes = Encryption.EncryptString(bytes, QiiwiApiConfig.ApiSecret, salt); 

			return encryptedJsonBytes;
			*/
		}

		public static string DecryptAndUnzipData(string data, byte[] salt)
		{
			var decryptedBytes = Encryption.ReadCompressedDataWithGzip(Convert.FromBase64String(data));

			byte[] encryptedJsonBytes = Encryption.DecryptString(System.Text.Encoding.UTF8.GetString(decryptedBytes), System.Text.Encoding.ASCII.GetBytes("C2BD366267E6DF64A0A73EBF74906E360F614166A7B6BDAB571DFEEA5EBF3C48"), salt);

			string responseString = System.Text.Encoding.UTF8.GetString(encryptedJsonBytes);

			return responseString;
		}

		public static byte[] ReadCompressedDataWithGzip(byte[] data)
		{
			using (MemoryStream input = new MemoryStream(data))
			{
				using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress, true))
				{
					return ReadFully(gzip);
				}
			}
		}

	}
}