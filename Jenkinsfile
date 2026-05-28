pipeline {
 agent any

 environment {
  SOLUTION_NAME = 'RetailEdiGateway.sln'
  WEB_PROJECT = 'src/RetailEdiGateway.Web/RetailEdiGateway.Web.csproj'
  PUBLISH_DIR = 'publish'
  IIS_PATH = 'C:\\inetpub\\wwwroot\\RetailEdiGateway'
  IIS_POOL = 'EdiGatewayPool'
  
  // Retrieve database connection string securely from Jenkins Credentials
  PROD_DB_CONN = credentials('PROD_DB_CONNECTION_STRING')
 }

 stages {
  stage('Restore') {
   steps {
    echo 'Restoring NuGet packages and local tools...'
    bat "dotnet tool restore"
    bat "dotnet restore ${SOLUTION_NAME}"
   }
  }

  stage('Build') {
   steps {
    echo 'Building solution in Release mode...'
    bat "dotnet build ${SOLUTION_NAME} --configuration Release --no-restore"
   }
  }

  stage('Migrate Database') {
   steps {
    echo 'Applying Entity Framework Core migrations to production database...'
    // Use local dotnet-ef tool via 'dotnet dotnet-ef'
    bat "dotnet dotnet-ef database update --project src/RetailEdiGateway.Infrastructure/RetailEdiGateway.Infrastructure.csproj --startup-project ${WEB_PROJECT} --configuration Release --connection \"${env.PROD_DB_CONN}\""
   }
  }

  stage('Test') {
   steps {
    echo 'Running unit and integration tests...'
    bat "dotnet test ${SOLUTION_NAME} --configuration Release --no-build --logger \"junit;LogFileName=test_results.xml\""
   }
   post {
    always {
     echo 'Publishing test results...'
     junit '**/test_results.xml'
    }
   }
  }

  stage('Publish') {
   steps {
    echo 'Publishing application...'
    bat "dotnet publish ${WEB_PROJECT} --configuration Release --output ${PUBLISH_DIR} --no-build"
   }
  }

  stage('Deploy') {
   steps {
    echo 'Deploying to IIS and setting up environment configurations...'
    powershell """
    \$appcmd = "\$env:SystemRoot\\System32\\inetsrv\\appcmd.exe"

    # 1. Stop the IIS Application Pool to release file locks
    Write-Output "Stopping Application Pool: ${env.IIS_POOL}"
    & \$appcmd stop apppool /apppool.name:"${env.IIS_POOL}" 2>\$null
    Start-Sleep -Seconds 5
    
    # 2. Clean the target IIS directory
    Write-Output "Cleaning target directory: ${env.IIS_PATH}"
    if (Test-Path "${env.IIS_PATH}") {
     Get-ChildItem -Path "${env.IIS_PATH}" -Recurse | Remove-Item -Force -Recurse
    } else {
     New-Item -ItemType Directory -Path "${env.IIS_PATH}" -Force
    }
    
    # 3. Copy published files to the destination
    Write-Output "Copying published files to: ${env.IIS_PATH}"
    Copy-Item -Path "${env.PUBLISH_DIR}\\*" -Destination "${env.IIS_PATH}" -Force -Recurse

    # 3.1 Ensure IIS Application exists
    Write-Output "Verifying IIS Application mapping..."
    if (!(& \$appcmd list app /path:"/RetailEdiGateway")) {
     & \$appcmd add app /site.name:"Default Web Site" /path:"/RetailEdiGateway" /physicalPath:"${env.IIS_PATH}" /applicationPool:"${env.IIS_POOL}"
    }
    
    # 4. Inject Production Environment Variables into IIS Application Pool
    Write-Output "Configuring IIS Application Pool Environment Variables..."
    
    # Remove existing variables first to avoid 'duplicate' errors (ignore failure if they don't exist)
    & \$appcmd set config -section:system.applicationHost/applicationPools /-"[name='${env.IIS_POOL}'].environmentVariables.[name='ASPNETCORE_ENVIRONMENT']" /commit:apphost 2>\$null
    & \$appcmd set config -section:system.applicationHost/applicationPools /-"[name='${env.IIS_POOL}'].environmentVariables.[name='ConnectionStrings__DefaultConnection']" /commit:apphost 2>\$null
    
    # Add Environment Variables
    & \$appcmd set config -section:system.applicationHost/applicationPools /+"[name='${env.IIS_POOL}'].environmentVariables.[name='ASPNETCORE_ENVIRONMENT',value='Production']" /commit:apphost
    & \$appcmd set config -section:system.applicationHost/applicationPools /+"[name='${env.IIS_POOL}'].environmentVariables.[name='ConnectionStrings__DefaultConnection',value='${env.PROD_DB_CONN}']" /commit:apphost

    # 5. Restart the Application Pool to apply settings
    Write-Output "Starting Application Pool: ${env.IIS_POOL}"
    & \$appcmd start apppool /apppool.name:"${env.IIS_POOL}" 2>\$null
    """
   }
  }
 }
}
