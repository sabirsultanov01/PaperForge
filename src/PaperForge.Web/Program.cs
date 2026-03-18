using PaperForge.BLL.Services;
using PaperForge.BLL.Services.Interfaces;
using PaperForge.DAL.Repositories;
using PaperForge.DAL.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Repositories (in-memory)
builder.Services.AddSingleton<IPaperRepository, PaperRepository>();
builder.Services.AddSingleton<ISectionRepository, SectionRepository>();
builder.Services.AddSingleton<IReferenceRepository, ReferenceRepository>();
builder.Services.AddSingleton<ITemplateRepository, TemplateRepository>();

// Services (BLL)
builder.Services.AddScoped<IPaperService, PaperService>();
builder.Services.AddScoped<ICitationService, CitationService>();
builder.Services.AddScoped<IOutlineGeneratorService, OutlineGeneratorService>();
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddHttpClient<ICrossRefService, CrossRefService>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
