using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SQLog
{
    class Program
    {

        static void parseaHora(string hora,out int h, out int m, out int s){
            string[] datos = hora.Split(':');
            h = Convert.ToInt16(datos[0]);
            m = Convert.ToInt16(datos[1]);
            s = Convert.ToInt16(datos[2]);
        }

         static void Main(string[] args)
        {
            int avisos = 0;
            int tandasLocales = 0;
            int nRelleno = 0;
            int cuentaTandas = 0;
            double mayorRelleno = 0;
            double totalRelleno = 0;
            int id;
            String nombre;
            String hora;
            bool relleno = false;
            bool mail = true;
            TimeSpan horaRelleno = new TimeSpan();
            TimeSpan horaStop = new TimeSpan();
            TimeSpan horaMayorRelleno = new TimeSpan();
            TimeSpan primerPlay = new TimeSpan();
            TimeSpan ultimoPlay = new TimeSpan();
            int h;
            int m;
            int s;
            DateTime ayer;
            SQLiteConnection conexion_sqlite;
            SQLiteCommand cmd_sqlite;
            SQLiteDataReader datareader_sqlite;
            List<int> lista = new List<int>();

            ayer = DateTime.Now.AddDays(-1);
            string archivo = ayer.ToString("dd-MM-yyyy") + ".sqlite";
            string anio = ayer.Year.ToString();
            string mes = ayer.Month.ToString();
            string ruta = ConfigurationManager.AppSettings["ruta"] + anio + "\\" + mes + "\\";

            string path = "";
            if (args.Length > 0)
                path = args[0];
            else
                path = ruta + archivo;

            //crearconexion a bd
            conexion_sqlite = new SQLiteConnection("Data Source =" + path + "; Version=3;");


            //Abrir conexion
            conexion_sqlite.Open();

            //Creando el comando SQL
            cmd_sqlite = conexion_sqlite.CreateCommand();

            //El Objeto SQLite

            cmd_sqlite.CommandText = "SELECT repro_Id, repro_Nombre, repro_Hora FROM log_reproduccion order by repro_Id";
            datareader_sqlite = cmd_sqlite.ExecuteReader();

            while (datareader_sqlite.Read())
            {
                id = datareader_sqlite.GetInt16(0);
                nombre = datareader_sqlite.GetString(1);
                hora = datareader_sqlite.GetString(2);
                if (nombre == "PLAY LOCAL")
                {
                    tandasLocales++;
                    lista.Add(datareader_sqlite.GetInt16(0));
                }
                //
                if (nombre.Contains("Pista"))
                {
                    relleno = true;
                    nRelleno++;
                    parseaHora(datareader_sqlite.GetString(2), out h, out m, out s);
                    horaRelleno = new TimeSpan(h, m, s);
                }

                if (nombre == "STOP LOCAL" && relleno)
                {
                    parseaHora(datareader_sqlite.GetString(2), out h, out m, out s);
                    horaStop = new TimeSpan(h, m, s);
                    totalRelleno = totalRelleno + horaStop.Subtract(horaRelleno).TotalSeconds;
                    if (horaStop.Subtract(horaRelleno).TotalSeconds > 0)
                    {
                        cuentaTandas++;
                    }
                    relleno = false;
                }
                if (horaStop.Subtract(horaRelleno).TotalSeconds > mayorRelleno)
                {
                    mayorRelleno = horaStop.Subtract(horaRelleno).TotalSeconds;
                    horaMayorRelleno = horaStop;
                }
                //cantidad de avisos emitidos
                if (!nombre.Equals("PLAY LOCAL") & !nombre.Equals("STOP LOCAL") & !nombre.StartsWith("Pista"))
                {
                    avisos++;
                }

                if (id == lista[0])
                {
                    parseaHora(datareader_sqlite.GetString(2), out h, out m, out s);
                    primerPlay = new TimeSpan(h, m, s);
                }


                if (id == lista[tandasLocales - 1])
                {
                    parseaHora(datareader_sqlite.GetString(2), out h, out m, out s);
                    ultimoPlay = new TimeSpan(h, m, s);
                }


            }


            //En Consola

            Console.WriteLine("FECHA... : " + ayer.ToString("dd-MM-yyyy"));
            Console.WriteLine("Primer Play.... : " + primerPlay.ToString());
            Console.WriteLine("Ultimo Play.... : " + ultimoPlay.ToString());
            Console.WriteLine("Total de avisos.... : " + avisos.ToString());
            Console.WriteLine("Total de tandas locales.... : " + tandasLocales.ToString());
            Console.WriteLine("Total de tandas con relleno : " + cuentaTandas.ToString());
            Console.WriteLine("Total de relleno : " + totalRelleno.ToString() + " seg.");
            Console.WriteLine("Promedio de relleno : " + (totalRelleno / cuentaTandas).ToString("N2") + " seg.");
            Console.WriteLine("Tanda con mayor relleno : " + horaMayorRelleno.ToString() + " Duracion : " + mayorRelleno + " seg.");

            //Cerrando Conexion

            conexion_sqlite.Close();

            //Ejecuta lo anterior

            String url = ConfigurationManager.AppSettings["url"];

            if (args.Length > 0)
                return;
            try
            {
                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection
                    {
                        {"emisora" , ConfigurationManager.AppSettings["emisora"]},
                        {"fecha" , ayer.ToString("dd-MM-yyyy")},
                        {"avisos", avisos.ToString()},
                        {"primerPlay", primerPlay.ToString()},
                        {"ultimoPlay" , ultimoPlay.ToString()},
                        {"tandasLocales" , tandasLocales.ToString()},
                        {"cuentaTandas" , cuentaTandas.ToString()},
                        {"totalRelleno" , totalRelleno.ToString()},
                        {"promedioRelleno" , (totalRelleno / cuentaTandas).ToString("N2")},
                        {"mayorRelleno" , horaMayorRelleno.ToString()},
                        {"duracionMRelleno" , mayorRelleno.ToString()},
                        {"correo" , ConfigurationManager.AppSettings["correo"]}
                    };

                    var response = wb.UploadValues(url, "POST", data);
                    //Console.WriteLine(Encoding.UTF8.GetString(response));
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("----ERROR----" + ex.Message);
            }
        }
    }
}
