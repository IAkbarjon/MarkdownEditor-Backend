# Этап сборки (используем .NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы и собираем проект
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Этап запуска (используем .NET 10 runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Настройка порта для Render
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "MarkdownEditor.dll"]