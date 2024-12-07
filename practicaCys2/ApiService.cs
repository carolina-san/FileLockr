using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseAddress;
    private string _authToken;

    // Constructor
    public ApiService(string baseAddress)
    {
        _httpClient = new HttpClient();
        _baseAddress = baseAddress.TrimEnd('/');
    }


    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest requestData)
    {
        try
        {
            HttpResponseMessage response;
            if (requestData is MultipartFormDataContent formData)
            {
                Console.WriteLine("es un form data");
                ImprimirDatosForm(formData);
                // Enviar los datos como multipart
                response = await _httpClient.PostAsync($"{_baseAddress}/{endpoint}", formData);
            }
            else
            {
                // Si es otro tipo de contenido (JSON), serializar y enviar como application/json
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{_baseAddress}/{endpoint}", content);
            }
            response.EnsureSuccessStatusCode();

            // Leer y deserializar la respuesta
            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Deserializar la respuesta en el tipo adecuado
            var result = JsonConvert.DeserializeObject<TResponse>(jsonResponse);

            return result;
        }
        catch (HttpRequestException httpEx)
        {
            throw new Exception($"Error al realizar la solicitud: {httpEx.Message}", httpEx);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al realizar la solicitud POST: {ex.Message}");
        }
    }
    public void ImprimirDatosForm(MultipartFormDataContent formData)
    {
        foreach (var content in formData)
        {
            // Imprimir el nombre del campo
            Console.WriteLine($"Campo: {content.Headers.ContentDisposition.Name}");

            // Si es un archivo, imprimir el nombre del archivo
            if (content is StreamContent fileContent)
            {
                Console.WriteLine($"Archivo: {content.Headers.ContentDisposition.FileName}");
            }
            else
            {
                // Si es un campo de texto, imprimir su valor
                var value = content.ReadAsStringAsync().Result;
                Console.WriteLine($"Valor: {value}");
            }
        }
    }
    // Método genérico para realizar solicitudes GET
    public async Task<TResponse> GetAsync<TResponse>(string endpoint)
    {
        try
        {
            // Realizar la solicitud GET
            var response = await _httpClient.GetAsync($"{_baseAddress}/{endpoint}");
            // Verificar si la respuesta es exitosa
            response.EnsureSuccessStatusCode();

            // Leer y deserializar la respuesta
            var jsonResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(jsonResponse);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al realizar la solicitud GET: {ex.Message}");
        }
    }


    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var loginData = new
        {
            nombre = username,
            clave = password
        };

        var response = await PostAsync<object, LoginResponse>("auth/login", loginData);

        // Manejar el resultado basado en el campo Status
        switch (response.Status)
        {
            case "success":
                break;
            case "error":
                break;
            default:
                throw new Exception("Respuesta inesperada del servidor.");
        }

        return response;
    }

    public async Task<LoginResponse> CreaUser(string username, string password, string salt,string publicKey, string privateKey)
    {
        var registerData = new
        {
            nombre = username,
            clave = password,
            salt = salt,
            publicKey = publicKey,
            privateKey = privateKey
        };

        // Usar el método POST para enviar las credenciales
        return await PostAsync<object, LoginResponse>("auth/register", registerData);
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            // Llamar al método GET para obtener la lista de usuarios
            return await GetAsync<List<User>>("usuarios");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de usuarios: {ex.Message}");
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<FicheroGet>> GetFicherosAsync()
    {
        try
        {
            // Llamar al método GET para obtener la lista de usuarios
            return await GetAsync<List<FicheroGet>>("ficheros");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de ficheros: {ex.Message}");
        }
    }

    public async Task<FicheroGet> GetFicheroAsync(int id)
    {
        try
        {
            // Llamar al método GET para obtener la lista de usuarios
            return await GetAsync<FicheroGet>($"ficheros/{id}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de ficheros: {ex.Message}");
        }
    }

    public async Task<int> GetFicheroId(string nombre)
    {
        try
        {
            // Llamar al método GET para obtener la lista de usuarios
           FicheroGet fichero = await GetAsync<FicheroGet>($"ficheros/{nombre}");

            return fichero.idFichero;

        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener el usuario: {ex.Message}");
        }
    }
    public async Task<FicheroPost> CreaFichero(string filePath, string nombre)
    {
        try
        {
            using (var httpClient = new HttpClient())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Crear el contenido del archivo
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Crear el contenido de la solicitud
                var form = new MultipartFormDataContent();
                form.Add(fileContent, "file", Path.GetFileName(filePath));

                // Realizar la solicitud al servidor en el puerto 3000
                var response3000 = await httpClient.PostAsync("http://localhost:3000/upload", form);

                if (!response3000.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error al subir el archivo al servidor 3000");
                    return null;
                }

                // Obtener la respuesta del servidor 3000 (presumiblemente JSON con nombre y ruta)
                var responseContent = await response3000.Content.ReadAsStringAsync();
                var fileData = JsonConvert.DeserializeObject<FicheroResponse>(responseContent);
                Console.WriteLine("el archivo ya se ha subido: " + fileData.nombre + fileData.ruta);
                // Subir nombre y ruta al servidor en el puerto 8080

                var form8080 = new
                {
                    nombre = fileData.nombre,
                    archivo = fileData.ruta
                };
                return await PostAsync<object, FicheroPost>("ficheros", form8080);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al procesar la solicitud: {ex.Message}");
            return null;
        }
    }
    public async Task<int> GetUserId(string username)
    {
        try
        {
            // Llamar al método GET para obtener la lista de usuarios
            User usuario = await GetAsync<User>($"usuario/{username}");
            
            return usuario.idUsuario;

        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener el usuario: {ex.Message}");
        }
    }

    public async Task<User> GetUser(int id)
    {
        try
        {
            User usuario = await GetAsync<User>($"usuarios/{id}");
            return usuario;

        }
        catch (Exception ex)
        {
            return null;
        }
    }
    public async void CompartirFichero(int id,int user,string kfile,string iv)
    {
        Console.WriteLine("contenido recibido en compartir fichero: " + id + user + kfile + iv);
        var registerData = new
        {
            archivo = id,
            usuario = user,
            kfile = kfile,
            iv = iv

        };
        await PostAsync<object,FicheroResponse>("compartir", registerData);
    }
    public async Task<List<FicheroGet>> getFicheros(int usuario)
    {
        try
        {

            List<FicheroGet> ficheros = await GetAsync<List<FicheroGet>>($"compartir/{usuario}");

            return ficheros;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de ficheros: {ex.Message}");
        }
    }

    public async Task<byte[]> getArchivosServer(string nombre)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                
                var response = await httpClient.GetAsync($"http://localhost:3000/upload/{nombre}");
                response.EnsureSuccessStatusCode();

                // Leer y deserializar la respuesta
                var fileBytes = await response.Content.ReadAsByteArrayAsync(); // Lee el archivo binario

                return fileBytes; // Devuelve los bytes del archivo
            }
                
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de ficheros: {ex.Message}");
        }
    }

    public async Task<Compartir> getFicheroClaves(int idFichero,int idUsuario)
    {
        try
        {
            Compartir fichero = await GetAsync<Compartir>($"compartir?idFichero={idFichero}&idUsuario={idUsuario}");

            return fichero;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener la lista de ficheros: {ex.Message}");
        }
    }
}


public class LoginResponse
{
    public string Status { get; set; } // 'success' o 'error'
    public string Message { get; set; } // Mensaje descriptivo ('Usuario no encontrado', 'Contraseña incorrecta')
    public string Token { get; set; } // Token si el login es exitoso
}
public class FicheroResponse
{
    public string nombre { get; set; }
 
    public string ruta { get; set; }

}
public class User
{
    public int idUsuario { get; set; }
    public string nombre { get; set; }

    public string clave { get; set; }

    public string salt { get; set; }
    public string publicKey { get; set; }
    public string privateKey { get; set; }
}


public class Key1
{
    public string type { get; set; }
    public string data { get; set; }
}

public class FicheroPost
{
    public string message { get; set; }

    public int idFichero { get; set; }
}
public class FicheroGet
{
    public int idFichero { get; set; }
    public string nombre { get; set; }
    public string archivo { get; set; }
}
public class Archivo
{
    public string type { get; set; }
    public byte[] data { get; set; }
}

public class Compartir
{
    public int archivo { get; set; }
    public int usuario { get; set; }
    public string kfile { get; set; }
    public string iv { get; set; }
}