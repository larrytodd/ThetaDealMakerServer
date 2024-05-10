var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddJsonOptions(opt=>opt.JsonSerializerOptions.PropertyNamingPolicy=null);
builder.Services.AddCors(
    p => p.AddPolicy("production", b => {
      b.WithOrigins("https://*.thetadealmaker.io","https://thetadealmaker.io","https://www.thetadealmaker.io")
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyMethod()
        .AllowAnyHeader();
    })
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    //TODO:Remove localhost, before going live. This is needed to debug client locally
    app.UseCors(builder =>
      {
        builder
              .WithOrigins("http://localhost:5173","https://thetadealmaker.io","https://data.thetadealmaker.io","https://www.thetadealmaker.io")
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithMethods("GET", "PUT", "POST", "DELETE", "OPTIONS")
              .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
 
      }
    );
}else{
    app.UseCors(builder =>
      {
        builder
              .WithOrigins("http://localhost:5173","https://thetadealmaker.io","https://data.thetadealmaker.io","https://www.thetadealmaker.io")
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithMethods("GET", "PUT", "POST", "DELETE", "OPTIONS")
              .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
 
      }
    );
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
