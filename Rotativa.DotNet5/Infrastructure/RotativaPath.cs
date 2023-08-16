using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Rotativa.Infrastructure;
internal class RotativaPath
{
	private readonly IWebHostEnvironment _environment;
	private string rotativaPath;
	public RotativaPath(IWebHostEnvironment env)
	{
		_environment = env;
	}

	internal string Path
	{
		get
		{
			if (rotativaPath is not null) return rotativaPath;

			if (_environment.EnvironmentName == "Development")
			{
				rotativaPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly( ).Location), "wwwroot", "Rotativa");
			}
			else
			{
				rotativaPath = System.IO.Path.Combine(_environment.WebRootPath, "Rotativa");
			}

			if (!Directory.Exists(rotativaPath))
			{
				throw new ApplicationException("Folder containing wkhtmltopdf.exe not found, searched for " + rotativaPath);
			}

			return rotativaPath;
		}
	}
}
