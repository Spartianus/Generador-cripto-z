using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Generador_Cripto_Z
{
    internal class Program
    {
        static string connectionString = "Server=localhost\\SQLEXPRESS01;Database=sistemaclinico;Trusted_Connection=True;";

        // Clave y vector para AES (deben ser los mismos que en el sistema)
        private static readonly byte[] aesKey = Encoding.UTF8.GetBytes("1234567890ABCDEF1234567890ABCDEF"); // 32 caracteres
        private static readonly byte[] aesIV = Encoding.UTF8.GetBytes("ABCDEF1234567890"); // 16 caracteres

        static void Main()
        {
            Console.Title = "🔐 Generador Cripto Z - Sistema Rafael";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================");
            Console.WriteLine("     🔑 Generador Cripto Z - Sistema Rafael");
            Console.WriteLine("===============================================");
            Console.ResetColor();

            Console.Write("Cantidad de claves a generar: ");
            if (!int.TryParse(Console.ReadLine(), out int count)) count = 1;

            Console.Write("Días de validez (0 = indefinido): ");
            int days = 0; int.TryParse(Console.ReadLine(), out days);

            Console.Write("Máx. activaciones (0 = ilimitado): ");
            int maxAct = 0; int.TryParse(Console.ReadLine(), out maxAct);

            Console.WriteLine("\nGenerando claves...\n");

            for (int i = 0; i < count; i++)
            {
                string key = GenerateKey(); // e.g. SR-AB12-CD34-EF56
                string keyHash = HashKeyHex(key);
                string claveCifrada = EncriptarClave(key);

                // Muestra la clave generada
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Clave #{i + 1}: {key}");
                Console.ResetColor();

                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string sql = @"INSERT INTO sistemaclinico.LicenciasCatalogo
                                       (ClaveTexto, ExpirationDays, MaxActivations, Activations, IsActive)
                                       VALUES (@ClaveTexto, @ExpirationDays, @MaxActivations, 0, 1)";
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@ClaveTexto", claveCifrada);
                            cmd.Parameters.AddWithValue("@ExpirationDays", days == 0 ? (object)DBNull.Value : days);
                            cmd.Parameters.AddWithValue("@MaxActivations", maxAct == 0 ? (object)DBNull.Value : maxAct);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error al guardar clave en la base de datos: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n✅ Generación finalizada.");
            Console.ResetColor();
            Console.WriteLine("Guarda las claves mostradas en un lugar seguro.");
            Console.WriteLine("\nPresiona una tecla para salir...");
            Console.ReadKey();
        }

        static string GenerateKey()
        {
            // Genera un key estilo: SR-XXXX-XXXX-XXXX
            string rnd = RandomString(12).ToUpper(); // 12 caracteres
            return $"SR-{rnd.Substring(0, 4)}-{rnd.Substring(4, 4)}-{rnd.Substring(8, 4)}";
        }

        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // evita confusión con O/0/I/1
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                var sb = new StringBuilder(length);
                for (int i = 0; i < length; i++)
                    sb.Append(chars[bytes[i] % chars.Length]);
                return sb.ToString();
            }
        }

        static string HashKeyHex(string key)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        static string EncriptarClave(string textoPlano)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(textoPlano);
                    sw.Close();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}
