using Sophia.Web.Apoio;

var builder = WebApplication.CreateBuilder(args);
builder.AdicionarSophia();

var app = builder.Build();
app.UsarSophia();

await app.RunAsync();
