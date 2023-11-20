using UnMango.Wishlists.Api.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGraphQLServer()
	.AddQueryType<Query>();

const string localhostPolicy = "localhost";
builder.Services.AddCors(o => o.AddPolicy(
	localhostPolicy,
	p => p.WithOrigins("http://localhost:5173", "http://localhost:8080")
		.WithHeaders("*")));

var app = builder.Build();

if (!app.Environment.IsProduction()) {
	app.UseCors(localhostPolicy);
}

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
