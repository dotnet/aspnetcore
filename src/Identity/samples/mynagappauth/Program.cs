// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors();

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite("Data Source=tmpauth.sqlite3"));
builder.Services.AddIdentityEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// demo stuff here, never do this in production
var ctx = app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
ctx.Database.EnsureDeleted();
ctx.Database.EnsureCreated();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors(corsBuilder => corsBuilder
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader().AllowAnyMethod().AllowCredentials());

app.UseHttpsRedirection();

app.MapGet("/effects", (ClaimsPrincipal user) =>
    new
    {
        UserName = user?.Identity?.Name ?? string.Empty,
        Effects = new string[]
        {
            @"c.width|=0;for(b=1e3;b--;){r=i=>i*50+550;d=r(C(b+t*2)+T(p=b/4));e=r(S(b+t/2)+T(p));x.fillStyle=R(p/2,p,b/4,0.2);x.fillRect(d,e,10,10);}",
            @"c.width|=0;for(b=0;b<2e3;b++){d=(f=C(b/2.15+t))*b+800;e=S(b/2.16+t)*b+600;x.fillStyle=`hsla(${S(b/4)*255},100%,50%,1)`;x.fillRect(d,e,9,9);}",
            @"for(i=p=1e3;i--;){x.fillStyle=R(S(t+i/500)*p/2,C(t+i/500)*p/2,T(t+i/500)*p/2,0.1);x.fillRect((o=(t%1)*p/2)+S((z=t+i/10))*o,o+C(z)*o,50,50);}",
            @"c.width|=j=p=500;while(j>(z=255)){for(i=p;i>0;i--){x.fillStyle=R(S(i)*z,C(i)*z,T(i)*z,.5);x.fillRect(S(i+t)*j+p,C(i+t*3)*j+p,15,15);}j-=10}",
            @"f=(w,a)=>{x.fillStyle=R(S(a)*255,C(a)*255,T(a)*255,0.01);x.fillRect(a,0,64,w);};for(i=2e3;i-=4;)f(C(i+t)*1e3,i);x.rotate((t%1)*Math.PI);",
            @"b=540;x.beginPath();x.strokeStyle=R(S(t)*255,C(t)*255,64,0.1);x.moveTo(S(t)*b*2+b,C(t)*b+b);x.lineTo(C(t+90)*b*2+b,S(t+180)*b+b);x.stroke();",
            @"i=500;c.width|=0;r=()=>Math.random()*20-9;b=(d,e,f)=>{x.fillStyle=R(0,0,99,f/i);x.fillRect(d,e,10,10);if(f){b(d+r(),e+r(),f-1);}};b(9,99,i);",
            @"for(c.width=b=1800;b;b-=20)for(d=1000;d;d-=20){x.fillStyle=R(S(b*d+t*2)*240,C(b*d+t*3)*200,S(t*4)*64+C(t*2)*64,0.2);x.fillRect(b,d,40,40);}",
             @"c.width|=0;for(i=120;i--;){p=q=1;for(j=30;j--;)p+=q,q=t/3,x.fillStyle=R(S(j)*255,C(j)*255,j*16,0.1),x.fillRect((j*p*10+i)%2e3,i+200,5,600);}",
        }
    }).RequireAuthorization(); 

app.MapGroup("/identity").MapIdentity<IdentityUser>();

app.UseStaticFiles();

app.Run();
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
