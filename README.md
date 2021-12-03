# Azure Auth and ARM Demo

## Introduction

This demo shows how to implement oauth2.0 with Azure Active Directory and how to use the access token with Azure
Resource Manager to deploy resources on behalf of the user.

## Requirements

- DotNet 5.0

## Running the demo

1. Create App Registration
    - For supported account types choose "Accounts in any organizational directory and personal Microsoft accounts
    - For local demo, set the redirect url to 'https://localhost:5001/auth/callback'
    - Note down client id of the created App Registration
    - Go to API Permissions and add a permission for
        - Azure Service Management user_impersonation
    - Go to Certificates & Secrets and generate a new client secret and note down the value of the client secret
    - Go to Authenticate and a front channel logout url of 'https://localhost:5001/auth/signout'
2. Edit the config
    - In appsettings.json
        - change 'ClientId' to the client id you obtained in step 1
        - change 'ClientSecret' to the client secret you obtained in step 1
2. Build and run the demo
    - `cd src`
    - `dotnet build`
    - `dotnet run`
3. Launch the demo
    - Go to https://localhost:5001 in your browser