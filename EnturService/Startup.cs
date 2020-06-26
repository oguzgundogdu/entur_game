using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnturBusiness;
using EnturEntity;
using EnturService.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EnturService
{
	public class Startup
	{
		private static readonly object _syncRoot = new object();

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }
		private ServiceProvider _serviceProvider;
		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors( options =>
			{
				options.AddDefaultPolicy( builder =>
				{
					builder.WithOrigins( "*" )
					.AllowAnyMethod()
					.AllowAnyHeader();
				} );
			} );
			services.AddControllers();

			services.AddScoped<EnturTransaction>();
			services.AddScoped<IWordManager, WordManager>();
			services.AddScoped<IUserManager, UserManager>();
			services.AddScoped<IGameManager, GameManager>();
			_serviceProvider = services.BuildServiceProvider();

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			app.UseCors();
			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseEndpoints( endpoints =>
			 {
				 endpoints.MapControllers();
			 } );

			app.UseWebSockets();
			app.Use( async (ctx, nextMsg) =>
			{
				if (ctx.Request.Path == "/ListenGame")
				{
					if (ctx.WebSockets.IsWebSocketRequest)
					{
						var wSocket = await ctx.WebSockets.AcceptWebSocketAsync();
						await Talk( ctx, wSocket, _serviceProvider );
					}
					else
					{
						ctx.Response.StatusCode = 400;
					}

				}
				else
				{
					await nextMsg();
				}
			} );


		}

		private async Task Talk(HttpContext hContext, WebSocket wSocket, IServiceProvider serviceProvider)
		{
			var bag = new byte[1024];
			WebSocketReceiveResult result = await wSocket.ReceiveAsync( new ArraySegment<byte>( bag ), CancellationToken.None );
			SocketResponse response = new SocketResponse();

			while (!result.CloseStatus.HasValue)
			{
				var inComingMesage = Encoding.UTF8.GetString( bag, 0, result.Count );

				if (GameContext.Current == null)
				{
					lock (_syncRoot)
					{
						if (GameContext.Current == null)
						{
							GameContext.CreateGame( serviceProvider );
						}
					}
				}

				int userId = Convert.ToInt32( inComingMesage.Trim() );
				string message;
				response.GameContent = GameContext.Current.RequestQuestion( userId, out message );
				response.Message = message;
				response.Success = true;

				string strJson = JsonConvert.SerializeObject( response );
				byte[] outGoingMessage = Encoding.UTF8.GetBytes( strJson );
				await wSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );
				result = await wSocket.ReceiveAsync( new ArraySegment<byte>( bag ), CancellationToken.None );
			}

			await wSocket.CloseAsync( result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None );
		}
	}
}
