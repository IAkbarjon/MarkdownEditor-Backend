# Этап сборки (выберите версию SDK, соответствующую вашему проекту, например 8.0 или 9.0)
FROM ://microsoft.com AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы и собираем проект
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Этап запуска (используем легковесный runtime)
FROM ://microsoft.com AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Render динамически назначает порт через переменную среды PORT. 
# Говорим ASP.NET слушать этот порт.
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "MarkdownEditor.dll"]
