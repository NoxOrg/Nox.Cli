FROM mcr.microsoft.com/dotnet/aspnet:6.0-jammy
EXPOSE 8000

RUN apt-get update

RUN apt-get install -y tzdata

ENV TZ Africa/Johannesburg

COPY ./bin/Release/net6.0/publish app/
WORKDIR /app
RUN sed -i '1i openssl_conf = default_conf' /etc/ssl/openssl.cnf && echo "\n[ default_conf ]\nssl_conf = ssl_sect\n[ssl_sect]\nsystem_default = system_default_sect\n[system_default_sect]\nMinProtocol = TLSv1.2\nCipherString = DEFAULT:@SECLEVEL=1" >> /etc/ssl/openssl.cnf
ENTRYPOINT ["dotnet", "Nox.Cli.Server.dll"]
