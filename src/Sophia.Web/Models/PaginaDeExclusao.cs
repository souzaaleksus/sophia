namespace Sophia.Web.Models;

public sealed record PaginaDeExclusao(
    int Id,
    string Titulo,
    string Aviso,
    string Nome,
    string RotuloDoDetalhe,
    string Detalhe);
