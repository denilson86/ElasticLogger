using ElasticLogger.Interfaces;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace ElasticLogger.Serialize
{
    public class ProtectedSerializer
    {
        private static readonly byte[] PublicKey = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private static readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static ProtectedSerializer Instance = new ProtectedSerializer();

        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
        };

        private static readonly string[] unsafeWords =
        {
            /* "chr", "select", "insert", "union",
             "alert", "javascript", "console",
             "xhttp", "send", "update", "delete","escape"*/
        };

        private ProtectedSerializer()
        {
        }

        public string EncryptProperty(string plainText, string passPhrase)
        {
            var clearBytes = Encoding.Unicode.GetBytes(plainText);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(passPhrase, PublicKey);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }

                    plainText = Convert.ToBase64String(ms.ToArray());
                }
            }

            return plainText;
        }

        public string DecriptProperty(string cipherText, string passPhrase)
        {
            cipherText = cipherText.Replace(" ", "+");
            var cipherBytes = Convert.FromBase64String(cipherText);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(passPhrase, PublicKey);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }

                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }

            return cipherText;
        }

        public static string SerializeObject(object value)
        {
            if (value == null) return null;

            return JsonConvert.SerializeObject(value, value.GetType(), JsonSettings);
        }

        public static T DeserializeObject<T>(string value)
        {
            if (value == null) return default;

            return JsonConvert.DeserializeObject<T>(value, JsonSettings);
        }

        public static object DeserializeObject(string value, Type type)
        {
            if (value == null) return null;

            return JsonConvert.DeserializeObject(value, type, JsonSettings);
        }

        public static Guid CreateMd5Hash(string unique)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.Default.GetBytes(unique));
                var result = new Guid(hash);
                return result;
            }
        }

        public static string CreateSha1Hash(string unique)
        {
            byte[] result;
            SHA512 shaM = new SHA512Managed();
            result = shaM.ComputeHash(Encoding.Default.GetBytes(unique));
            return Convert.ToBase64String(result);
        }

        public static string ToJwe(Encoding encoding, byte[] sessionSignature, byte[] userSignature, object @object, string deviceId)
        {
            var encripted = Encript(sessionSignature, userSignature, @object);
            var jwt = JWT.Encode(new Dictionary<string, object>
            {
                ["hash"] = encripted,
            }, Encoding.UTF8.GetBytes(deviceId), JwsAlgorithm.HS512);
            return jwt;
        }

        public static string Encript(ISession session, object @object)
            => Encript(session.SessionSignature, session.UserSignature, @object);

        public static string Encript(byte[] sessionSignature, byte[] userSignature, object @object)
        {
            var json = SerializeObject(@object ?? new object());
            using (var aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                var encryptor = aes.CreateEncryptor(sessionSignature, userSignature);
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cryptoStream))
                        {
                            sw.Write(json);
                        }

                        var encrypted = memoryStream.ToArray();
                        var b64 = Convert.ToBase64String(encrypted);
                        return b64;
                    }
                }
            }
        }


        public static object Decript(byte[] sessionSignature, byte[] userSignature, Type type, string token)
        {
            try
            {
                using (var aes = new AesManaged())
                {
                    var decryptor = aes.CreateDecryptor(sessionSignature, userSignature);
                    var bytes = Convert.FromBase64String(token);
                    using (var ms = new MemoryStream(bytes))
                    {
                        using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(cryptoStream))
                            {
                                var json = reader.ReadToEnd();
                                if (json == "{}")
                                    return null;
                                var expression = json.ToLowerInvariant();
                                for (var index = 0; index < unsafeWords.Length; index++)
                                    if (expression.Contains(unsafeWords[index]))
                                        //Todo: Disparar alerta de segurança
                                        return null;
                                var descrip = JsonConvert.DeserializeObject(json, type, JsonSettings);
                                return descrip;
                            }
                        }
                    }
                }
            }
            catch
            {
                return token;
            }
        }

        public static string RandomId(int size)
        {
            var data = RandomBytes(size);
            var result = new StringBuilder(size);
            foreach (var b in data) result.Append(chars[b % chars.Length]);
            return result.ToString();
        }

        public static byte[] RandomBytes(int size)
        {
            var data = new byte[size];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }

            return data;
        }
    }
}