# Этап сборки (используем .NET 8 SDK для компиляции)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы и собираем проект
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Этап запуска (используем легковесный runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Настройка порта для Render
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "MarkdownEditor.dll"]