using Dapper;
using BCrypt.Net;
using Microsoft.Data.Sqlite;
using TicketApp.Models;

namespace TicketApp.Data;

public class TicketRepository
{
    private readonly string _connectionString;

    public TicketRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "Data Source=tickets.db";
        InitializeDatabase();
    }

    //Funcion que permite inicializar la BD para activar la concurrencia, y en caso de sere primera vez crear tablas y usuario default
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        
        //Al usar SQLite, se habilita WAL para alta concurrencia.
        connection.Execute("PRAGMA journal_mode=WAL;");

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Usuarios (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT, PasswordHash TEXT);
            CREATE TABLE IF NOT EXISTS Tickets (Id INTEGER PRIMARY KEY AUTOINCREMENT, Asunto TEXT, Descripcion TEXT, Nombre TEXT, Correo TEXT, Telefono TEXT, Estatus TEXT, FechaCreacion DATETIME, ResueltoPor TEXT);
            CREATE TABLE IF NOT EXISTS ArchivosAdjuntos (Id INTEGER PRIMARY KEY AUTOINCREMENT, TicketId INTEGER, NombreOriginal TEXT, RutaAlmacenamiento TEXT, ContentType TEXT, EsEvidenciaSoporte BOOLEAN);
            CREATE TABLE IF NOT EXISTS Comentarios (Id INTEGER PRIMARY KEY AUTOINCREMENT, TicketId INTEGER, Texto TEXT, Autor TEXT, Fecha DATETIME);
            INSERT OR IGNORE INTO sqlite_sequence (name, seq) VALUES ('Tickets', 1000);
        ");
        
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Usuarios") == 0)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            
            connection.Execute("INSERT INTO Usuarios (Username, PasswordHash) VALUES (@Username, @PasswordHash)", 
                new { Username = "admin", PasswordHash = passwordHash });
        }
    }

    public async Task<int> CrearTicketAsync(Ticket ticket)
    {
        using var connection = new SqliteConnection(_connectionString);
        // Pensando en usuarios concurrentes, se utiliza RETURNING para evitar conflictos de id duplicado.
        var sql = @"INSERT INTO Tickets (Asunto, Descripcion, Nombre, Correo, Telefono, Estatus, FechaCreacion) 
                    VALUES (@Asunto, @Descripcion, @Nombre, @Correo, @Telefono, @Estatus, @FechaCreacion)
                    RETURNING Id;";
        return await connection.ExecuteScalarAsync<int>(sql, ticket);
    }

    public async Task GuardarArchivoAsync(ArchivoAdjunto archivo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO ArchivosAdjuntos (TicketId, NombreOriginal, RutaAlmacenamiento, ContentType, EsEvidenciaSoporte) 
            VALUES (@TicketId, @NombreOriginal, @RutaAlmacenamiento, @ContentType, @EsEvidenciaSoporte);", archivo);
    }

    public async Task<IEnumerable<Ticket>> ObtenerTicketsAsync(string? estatus = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = "SELECT * FROM Tickets";
        if (!string.IsNullOrEmpty(estatus)) sql += " WHERE Estatus = @Estatus";
        sql += " ORDER BY FechaCreacion DESC";
        return await connection.QueryAsync<Ticket>(sql, new { Estatus = estatus });
    }

    public async Task<IEnumerable<ArchivoAdjunto>> ObtenerArchivosPorTicketAsync(int ticketId)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<ArchivoAdjunto>("SELECT * FROM ArchivosAdjuntos WHERE TicketId = @TicketId", new { TicketId = ticketId });
    }

    public async Task ActualizarEstatusTicketAsync(int ticketId, string estatus, string? usuario)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = "UPDATE Tickets SET Estatus = @Estatus";
        if (estatus == "Resuelto") sql += ", ResueltoPor = @Usuario";
        sql += " WHERE Id = @Id";
        
        await connection.ExecuteAsync(sql, new { Estatus = estatus, Usuario = usuario, Id = ticketId });
    }
    
    public async Task<ArchivoAdjunto?> ObtenerArchivoAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<ArchivoAdjunto>("SELECT * FROM ArchivosAdjuntos WHERE Id = @Id", new { Id = id });
    }
    
    //Funcion que sirve para validar el login del usuario de soporte.
    public async Task<bool> ValidarUsuarioAsync(string username, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        // Buscamos el hash del usuario en la BD
        var hashEnBaseDeDatos = await connection.ExecuteScalarAsync<string>(
            "SELECT PasswordHash FROM Usuarios WHERE Username = @Username", 
            new { Username = username });

        // Si el usuario no existe, o si la contraseña no coincide con el hash, fallamos
        if (string.IsNullOrEmpty(hashEnBaseDeDatos)) return false;

        // BCrypt verifica matemáticamente si "admin123" corresponde a ese hash complejo
        return BCrypt.Net.BCrypt.Verify(password, hashEnBaseDeDatos);
    }
    
    public async Task<IEnumerable<Usuario>> ObtenerUsuariosAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Usuario>("SELECT Id, Username FROM Usuarios ORDER BY Username");
    }

    public async Task CrearUsuarioAsync(string username, string password)
    {
        // Hashear la contraseña antes de guardar
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
    
        using var connection = new SqliteConnection(_connectionString);
        var sql = "INSERT INTO Usuarios (Username, PasswordHash) VALUES (@Username, @Hash)";
        await connection.ExecuteAsync(sql, new { Username = username, Hash = passwordHash });
    }
    public async Task ActualizarPasswordAsync(int id, string nuevaClave)
    {
        // Hashear la nueva contraseña
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
    
        using var connection = new SqliteConnection(_connectionString);
        var sql = "UPDATE Usuarios SET PasswordHash = @Hash WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Hash = passwordHash, Id = id });
    }
    public async Task AgregarComentarioAsync(ComentarioTicket comentario)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = "INSERT INTO Comentarios (TicketId, Texto, Autor, Fecha) VALUES (@TicketId, @Texto, @Autor, @Fecha)";
        await connection.ExecuteAsync(sql, comentario);
    }

    public async Task<IEnumerable<ComentarioTicket>> ObtenerComentariosAsync(int ticketId)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<ComentarioTicket>(
            "SELECT * FROM Comentarios WHERE TicketId = @TicketId ORDER BY Fecha ASC", 
            new { TicketId = ticketId });
    }
}