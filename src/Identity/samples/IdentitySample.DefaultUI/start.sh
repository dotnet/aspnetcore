#!/bin/bash

# Identity V3 Schema - Docker Quick Start Script

# Set environment variable to bypass OpenSSL 3.x compatibility issues
export OPENSSL_CONF=/dev/null

echo "=========================================="
echo "Identity V3 Schema - Docker Setup"
echo "=========================================="
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running!"
    echo "Please start Docker Desktop and try again."
    exit 1
fi

echo "✅ Docker is running"
echo ""

# Function to display menu
show_menu() {
    echo "Choose an option:"
    echo ""
    echo "  1) Start Everything (SQL Server + Web App)"
    echo "  2) Start SQL Server Only"
    echo "  3) Stop All Containers"
    echo "  4) Stop and Remove All Data"
    echo "  5) View Logs"
    echo "  6) Check Database Schema"
    echo "  7) Connect to SQL Server (sqlcmd)"
    echo "  8) Rebuild and Restart"
    echo "  9) Exit"
    echo ""
    read -p "Enter option [1-9]: " choice
}

# Function to check SQL Server health
check_sql_health() {
    echo "Checking SQL Server health..."
    docker exec identity-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P YourStrong@Passw0rd -No \
        -Q "SELECT @@VERSION" 2>/dev/null

    if [ $? -eq 0 ]; then
        echo "✅ SQL Server is healthy"
        return 0
    else
        echo "❌ SQL Server is not ready yet"
        return 1
    fi
}

# Main loop
while true; do
    show_menu

    case $choice in
        1)
            echo ""
            echo "🚀 Starting SQL Server and Web Application..."
            docker compose up --build -d
            echo ""
            echo "Waiting for SQL Server to be ready..."
            sleep 5

            attempts=0
            max_attempts=12
            while [ $attempts -lt $max_attempts ]; do
                if check_sql_health; then
                    break
                fi
                attempts=$((attempts+1))
                echo "Waiting... (attempt $attempts/$max_attempts)"
                sleep 5
            done

            echo ""
            echo "✅ Containers started!"
            echo ""
            echo "📱 Web Application: http://localhost:5000"
            echo "🗄️  SQL Server: localhost:1433"
            echo "   Username: sa"
            echo "   Password: YourStrong@Passw0rd"
            echo "   Database: IdentitySample_V3"
            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        2)
            echo ""
            echo "🗄️  Starting SQL Server only..."
            docker compose up sqlserver -d
            echo ""
            echo "Waiting for SQL Server to be ready..."
            sleep 5

            attempts=0
            max_attempts=12
            while [ $attempts -lt $max_attempts ]; do
                if check_sql_health; then
                    break
                fi
                attempts=$((attempts+1))
                echo "Waiting... (attempt $attempts/$max_attempts)"
                sleep 5
            done

            echo ""
            echo "✅ SQL Server started!"
            echo ""
            echo "Connection details:"
            echo "  Server: localhost,1433"
            echo "  Username: sa"
            echo "  Password: YourStrong@Passw0rd"
            echo ""
            echo "To run the web app locally:"
            echo "  dotnet run"
            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        3)
            echo ""
            echo "🛑 Stopping all containers..."
            docker compose stop
            echo "✅ Containers stopped"
            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        4)
            echo ""
            echo "⚠️  WARNING: This will remove all data!"
            read -p "Are you sure? (yes/no): " confirm
            if [ "$confirm" = "yes" ]; then
                echo "🗑️  Stopping and removing containers and volumes..."
                docker compose down -v
                echo "✅ All containers and data removed"
            else
                echo "Cancelled"
            fi
            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        5)
            echo ""
            echo "📋 Viewing logs (Ctrl+C to exit)..."
            echo ""
            docker compose logs -f
            ;;

        6)
            echo ""
            echo "🔍 Checking V3 Schema..."
            echo ""

            # Check if database exists
            echo "1. Checking if database exists..."
            docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P YourStrong@Passw0rd -No \
                -Q "SELECT name FROM sys.databases WHERE name = 'IdentitySample_V3'"

            echo ""
            echo "2. Checking AspNetUserLogins columns (should include 'Id')..."
            docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P YourStrong@Passw0rd -No \
                -d IdentitySample_V3 \
                -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUserLogins' ORDER BY ORDINAL_POSITION"

            echo ""
            echo "3. Checking primary key on AspNetUserLogins (should be 'Id')..."
            docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P YourStrong@Passw0rd -No \
                -d IdentitySample_V3 \
                -Q "EXEC sp_pkeys 'AspNetUserLogins'"

            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        7)
            echo ""
            echo "🔌 Connecting to SQL Server..."
            echo "   (Type 'EXIT' to return to menu)"
            echo ""
            docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P YourStrong@Passw0rd -No \
                -d IdentitySample_V3
            ;;

        8)
            echo ""
            echo "🔨 Rebuilding and restarting..."
            docker compose down
            docker compose up --build -d
            echo ""
            echo "Waiting for SQL Server to be ready..."
            sleep 5

            attempts=0
            max_attempts=12
            while [ $attempts -lt $max_attempts ]; do
                if check_sql_health; then
                    break
                fi
                attempts=$((attempts+1))
                echo "Waiting... (attempt $attempts/$max_attempts)"
                sleep 5
            done

            echo ""
            echo "✅ Rebuild complete!"
            echo "📱 Web Application: http://localhost:5000"
            echo ""
            echo "Press Enter to continue..."
            read
            ;;

        9)
            echo ""
            echo "👋 Goodbye!"
            exit 0
            ;;

        *)
            echo ""
            echo "❌ Invalid option. Please try again."
            echo ""
            sleep 2
            ;;
    esac
done
