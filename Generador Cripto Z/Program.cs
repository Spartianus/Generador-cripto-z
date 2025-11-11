using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Generador_Cripto_Z
{
    internal class Program
    {
        static string connectionString = "Server=localhost\\SQLEXPRESS;Database=sistemaclinico;Trusted_Connection=True;";

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
                                       (KeyHash, ClaveTexto, ExpirationDays, MaxActivations, CreatedBy)
                                       VALUES (@KeyHash, @ClaveTexto, @ExpirationDays, @MaxActivations, @CreatedBy)";
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@KeyHash", keyHash);
                            cmd.Parameters.AddWithValue("@ClaveTexto", key); // opcional o NULL
                            cmd.Parameters.AddWithValue("@ExpirationDays", days == 0 ? (object)DBNull.Value : days);
                            cmd.Parameters.AddWithValue("@MaxActivations", maxAct == 0 ? (object)DBNull.Value : maxAct);
                            cmd.Parameters.AddWithValue("@CreatedBy", Environment.UserName);
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
    }
}
