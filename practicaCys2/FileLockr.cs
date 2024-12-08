using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace practicaCys2
{
    public partial class FileLockr : Form
    {
        private string[] clavesRSA;
        private ApiService apiService = new ApiService("http://localhost:8080/");
        private List<User> usuariosCompartir = new List<User>();
        private List<FicheroGet> ficheros = new List<FicheroGet>();
        private string kdatosUsuario;
        public FileLockr()
        {
            InitializeComponent();
            this.BackColor = SystemColors.GradientInactiveCaption;
            panelListado.Visible = false;
            panelAcceso.Visible = true;
            panelCifrado.Visible = false;

            string path1 = (@"../../../../../archivos");
            string path2 = (@"../../../../../archivos_descomprimidos");
            string path3 = (@"../../../../../claves");


            if (!Directory.Exists(path1))
                Directory.CreateDirectory(path1);

            if (!Directory.Exists(path2))
                Directory.CreateDirectory(path2);

            if (!Directory.Exists(path3))
                Directory.CreateDirectory(path3);



        }


        private async void btn_Abrir_Click(object sender, EventArgs e)
        {
            compressAndEncrypt decryptAes = new compressAndEncrypt();
            string user = textBoxUser.Text;
            int userId = await apiService.GetUserId(user);
            if (listaArchivos.SelectedItem != null)
            {

                string outputPath = "";
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Selecciona la carpeta donde deseas guardar los archivos descomprimidos";
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        outputPath = folderDialog.SelectedPath;
                    }
                    else
                    {
                        MessageBox.Show("Operación cancelada por el usuario.");
                    }
                }
                FicheroGet encryptedFile;
                foreach (FicheroGet item in ficheros)
                {
                    if (listaArchivos.SelectedItem == item.nombre)
                    {
                        encryptedFile = item;
                        // Buscar el archivo de claves que coincide con el nombre base

                        Compartir keys = await apiService.getFicheroClaves(encryptedFile.idFichero,userId);
                        User usuario = await apiService.GetUser(userId);
                        Console.WriteLine("Objeto compartir recibido");
                        // Obtener la clave (Kfile) y el IV desde el archivo comprimido
                        byte[] key = Convert.FromBase64String(keys.kfile);
                        byte[] iv = Convert.FromBase64String(keys.iv);
                        key = decryptAes.DecryptAesKeyWithRsa(key, clavesRSA[1]);
                        Console.WriteLine("kfile desencriptado");
                        iv = decryptAes.DecryptAesKeyWithRsa(iv, clavesRSA[1]);
                        // Desencriptar el archivo
                        byte[] archivoEncriptado= await apiService.getArchivosServer(encryptedFile.nombre);
                        byte[] decryptedFile = decryptAes.DecryptAes(archivoEncriptado, key, iv);

                        // Descomprimir el archivo desencriptado
                        Dictionary<string, byte[]> decompressedFiles = decryptAes.DecompressFiles(decryptedFile);

                        // Guardar los archivos descomprimidos
                        if (!Directory.Exists(outputPath))
                            Directory.CreateDirectory(outputPath);

                        foreach (var file in decompressedFiles)
                        {
                            Console.WriteLine("FILE KEY: " + file.Key + "PATH A ABRIR: " + outputPath);
                            File.WriteAllBytes(Path.Combine(outputPath, file.Key), file.Value);
                        } 
                        Process.Start("explorer.exe", outputPath);
                    }
                }
                
            }
        }

        //private async Task<bool> checkUser(string user, string passLogin)
        //{
        //    User usuario = await this.apiService.GetUser(user);
        //    return CryptographicOperations.FixedTimeEquals(
        //                                                    Convert.FromBase64String(usuario.clave), 
        //                                                    Convert.FromBase64String(passLogin)
        //                                                   );
        //}

        static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16]; // Tamaño del salt (128 bits)
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        static byte[] GenerateHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // Tamaño del hash (256 bits)
            }
        }

        static string ComputeSha256Hash(string input)
        {
            // Convertir el string en un arreglo de bytes
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            // Crear instancia de SHA256
            using (SHA256 sha256 = SHA256.Create())
            {
                // Generar el hash
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // Convertir el arreglo de bytes a un string hexadecimal
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        static (string, string) DividirPorMitad(string input)
        {
            // Calcular el punto medio
            int longitud = input.Length;
            int mitad = longitud / 2;

            // Obtener las dos mitades
            string primeraMitad = input.Substring(0, mitad);
            string segundaMitad = input.Substring(mitad);

            return (primeraMitad, segundaMitad);
        }


        private async void buttonAcceder_Click(object sender, EventArgs e)
        {
            compressAndEncrypt compressAndEncrypt = new compressAndEncrypt();
            string publicKey, privateKey;

            clavesRSA = new string[2];

            string passUsuario = textBoxPassword.Text;
            string user = textBoxUser.Text;

            if (string.IsNullOrEmpty(passUsuario) || string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Complete los campos de inicio de sesión.");
                panelListado.Visible = false;
                panelAcceso.Visible = true;
                panelCifrado.Visible = false;
                return;
            }

            // Hash y dividir la contraseña
            passUsuario = ComputeSha256Hash(passUsuario);
            (string kdatos, string passLogin) = DividirPorMitad(passUsuario);
            passLogin = Convert.ToBase64String(Encoding.UTF8.GetBytes(passLogin));
            this.kdatosUsuario = kdatos;
            try
            {
                // Intentar iniciar sesión
                LoginResponse login = await apiService.LoginAsync(user, passLogin);

                if (login.Status == "success") // Login exitoso
                {
                    apiService.SetAuthToken(login.Token);
                    apiService.SetAuthToken2(login.Token);
                    int id = await apiService.GetUserId(user);
                    User usuario = await apiService.GetUser(id);
                    clavesRSA[0] = usuario.publicKey;
                    byte[] privateKeyBytes = File.ReadAllBytes(@"../../../../../claves/" + user + "/privateKey.zip");
                    Dictionary<string, byte[]> privateKeyDict = compressAndEncrypt.DecompressFiles(privateKeyBytes);
                    clavesRSA[1] = compressAndEncrypt.DecryptPrivateKeyWithAes(privateKeyDict["privateKey"], kdatos); 
                    
                }
                else if (login.Message == "Usuario no encontrado") // Usuario no existe
                {
                    var result = MessageBox.Show("El usuario no existe. ¿Desea registrarlo?", "Usuario no encontrado",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Generar claves RSA
                        compressAndEncrypt.GenerateRsaKeys(out publicKey, out privateKey);
                        clavesRSA = new string[2] { publicKey, privateKey };
                        Directory.CreateDirectory(@"../../../../../claves/" + user);
                        // Crear claves públicas y privadas cifradas
                        byte[] clavePublica = Encoding.UTF8.GetBytes(publicKey);
                        byte[] clavePrivada = compressAndEncrypt.EncryptPrivateKeyWithAes(clavesRSA[1], kdatos);
                        clavePrivada = compressAndEncrypt.CompressFiles(new Dictionary<string, byte[]> { { "privateKey", clavePrivada } });
                        File.WriteAllBytes(@"../../../../../claves/" + user + "/privateKey" + ".zip", clavePrivada);
                        string encodedKey = Convert.ToBase64String(clavePrivada);
                        Console.WriteLine("Clave privada convertida a string: " + encodedKey);
                        // Generar hash y salt para la contraseña
                        byte[] saltRegistro = GenerateSalt();
                        byte[] hashRegistro = GenerateHash(passLogin, saltRegistro);

                        string salRegistro = Convert.ToBase64String(saltRegistro);
                        string passRegistro = Convert.ToBase64String(hashRegistro);

                        // Crear el usuario
                        LoginResponse registro = await apiService.CreaUser(user, passLogin, salRegistro, publicKey, encodedKey);
                        LoginResponse login2 = await apiService.LoginAsync(user, passLogin);
                        apiService.SetAuthToken(login2.Token);
                        apiService.SetAuthToken2(login2.Token);
                        int id = await apiService.GetUserId(user);
                        User usuario = await apiService.GetUser(id);
                        Console.WriteLine("Clave privada metida en clavesRSA[1]: " + clavesRSA[1]);
                        if (registro.Status == "success")
                        {
                            MessageBox.Show("Usuario registrado exitosamente. Puede iniciar sesión.");
                        }
                        else
                        {
                            MessageBox.Show($"Error al registrar usuario: {registro.Message}");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (login.Message == "Contraseña incorrecta") // Contraseña incorrecta
                {
                    MessageBox.Show("Contraseña incorrecta. Inténtelo nuevamente.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante el proceso de inicio de sesión: {ex.Message}");
                return;
            }

            // Mostrar panel listado y ocultar otros si todo fue exitoso
            panelListado.Visible = true;
            panelAcceso.Visible = false;
            panelCifrado.Visible = false;

            listarArchivos();
        }


        private async void buttonCifrar_Click(object sender, EventArgs e)
        {
            panelListado.Visible = false;
            panelAcceso.Visible = false;
            panelCifrado.Visible = true;
            listaExaminados.Items.Clear();
            usuariosCompartir.Clear();
            List<User> usuarios = await apiService.GetUsersAsync();
            listUsuarios.Items.Clear();
            List<User> seleccionados = new List<User>();
            foreach (var item in usuarios)
            {
                listUsuarios.Items.Add(item.nombre);
                if (item.nombre == textBoxUser.Text)
                    listUsuarios.SelectedItem = item.nombre;
                
            }

        }

        private async void addUser( string nombre)
        {
            
            int Iduser = await apiService.GetUserId(nombre);
            User user= await apiService.GetUser(Iduser);
            if (usuariosCompartir.Contains(user))
            {
                usuariosCompartir.Remove(user);
                Console.WriteLine("Usuario eliminado: " + user.nombre);
            }
            else
            {
                usuariosCompartir.Add(user);
                Console.WriteLine("Usuario añadido: " + user.nombre);
            }
            
            
        }

        
        private void buttonExaminar_Click(object sender, EventArgs e)
        {
            // Crear un diálogo para seleccionar archivos
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,  // Permitir seleccionar varios archivos
                Title = "Seleccionar archivos para comprimir y cifrar"
            };

            // Si el usuario selecciona archivos
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Agregar los archivos seleccionados al ListBox
                foreach (string filePath in openFileDialog.FileNames)
                {
                    listaExaminados.Items.Add(filePath);

                }

                MessageBox.Show("Archivos seleccionados. Presiona 'Confirmar' para cifrarlos.");
            }
        }

        private async void buttonConfirmar_Click(object sender, EventArgs e)
        {
         
            if (listaExaminados.Items.Count > 0)
            {
                string user = textBoxUser.Text;
                string nombreArchivo = textBoxNombreArchivo.Text;
                compressAndEncrypt compressAndEncrypt = new compressAndEncrypt();

                // Crear un diccionario para almacenar los archivos y su contenido
                Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

                // Recorrer los archivos del ListBox
                foreach (var item in listaExaminados.Items)
                {
                    string filePath = item.ToString();
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string fileName = Path.GetFileName(filePath);  // Obtener solo el nombre del archivo
                    files.Add(fileName, fileBytes);
                }

                // Generar una clave y IV aleatorios
                byte[] key = new byte[16]; // Kfile 
                RandomNumberGenerator.Fill(key); // Generar 128 bytes aleatorios para la clave

                byte[] iv = new byte[16]; // Iv
                RandomNumberGenerator.Fill(iv); // Generar 16 bytes aleatorios para el IV

                // Crear el diccionario que contendrá la clave (Kfile) y el IV
                
                
                string uniqueFileName = nombreArchivo + $"_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";

                byte[] encryptedZip = compressAndEncrypt.CompressAndEncryptFiles(files, key, iv);

                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{uniqueFileName}.zip");
                File.WriteAllBytes(tempFilePath, encryptedZip);
                try
                {
                    // Enviar el archivo al servidor
                    FicheroPost fichero = await apiService.CreaFichero(tempFilePath, uniqueFileName);
                    int idFichero = fichero.idFichero;
                    MessageBox.Show($"Archivos comprimidos y cifrados con éxito. ID del fichero: {idFichero}");

                    foreach (User usuario in usuariosCompartir)
                    {
                        Console.WriteLine("entra al for each: " + usuario.nombre);
                        byte[] kfile = compressAndEncrypt.EncryptAesKeyWithRsa(key, usuario.publicKey);
                        byte[] ivUser = compressAndEncrypt.EncryptAesKeyWithRsa(iv, usuario.publicKey);
                        string kfile2 = Convert.ToBase64String(kfile);
                        string ivString = Convert.ToBase64String(ivUser);
                        int idUsuario = await apiService.GetUserId(usuario.nombre);

                        apiService.CompartirFichero(idFichero, idUsuario, kfile2, ivString);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar el archivo: {ex.Message}");
                }
                finally
                {
                    // Limpiar el archivo temporal
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }


                // Limpiar el ListBox después de confirmar
                listaArchivos.Items.Clear();
                panelListado.Visible = true;
                panelAcceso.Visible = false;
                panelCifrado.Visible = false;


                listarArchivos();

            }
            else
            {
                MessageBox.Show("No se han seleccionado archivos. Por favor, selecciona los archivos antes de confirmar.");
            }
        }

        private void button_back_Click(object sender, EventArgs e)
        {
            panelListado.Visible = true;
            panelAcceso.Visible = false;
            panelCifrado.Visible = false;
            listarArchivos();
        }
        private async void listarArchivos()
        {
            listaArchivos.Items.Clear();
            ficheros.Clear();   
            string user = textBoxUser.Text;
            int idUser = await apiService.GetUserId(user);
            List<FicheroGet> fnuevos = await apiService.getFicheros(idUser);
            if (fnuevos == null)
            {
                MessageBox.Show("No se encontraron archivos para este usuario.");
            }
            else
            {
                foreach (FicheroGet archivo in fnuevos)
                {
                    ficheros.Add(archivo);
                    listaArchivos.Items.Add(archivo.nombre);
                }

            }


        }



        private async void compartir(object sender, EventArgs e)
        {

        }

        private void listUsuarios_SelectedIndexChanged(object sender, EventArgs e)
        {
            usuariosCompartir.Clear();
            if (listUsuarios.SelectedItem != null)
            {
                string selectedUser = listUsuarios.SelectedItem.ToString();
                foreach(string user in listUsuarios.SelectedItems)
                {
                    addUser(user);
                }
           
               
            }
        }
    }
}
