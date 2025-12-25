# Stage 1: Build the dotnet application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Define build argument for HTTPS certificate password
ARG CERT_PASS

# Set the working directory
WORKDIR /src

# Copy the project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application source code
COPY . ./

# Build the application
RUN dotnet publish -c Release -o /app/publish

# Generate and trust HTTPS development certificate
RUN mkdir ${HOME}/.dotnet/https && dotnet dev-certs https -ep "${HOME}/.dotnet/https/taskedin-be.pfx" -p ${CERT_PASS}
RUN dotnet dev-certs https --trust

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the publish stage
COPY --from=build /app/publish .

# Copy the HTTPS certificate from the build stage
COPY --from=build /root/.dotnet/https/taskedin-be.pfx ./https/taskedin-be.pfx

# Expose port 3001 for the application
EXPOSE 3001

# RUN dotnet ef database update

# Replace 'YourAppName.dll' with the actual DLL name of your application
ENTRYPOINT ["dotnet", "taskedin-be.dll"]