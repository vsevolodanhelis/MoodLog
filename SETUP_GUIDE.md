# MoodLog Setup Guide - Complete Installation Manual

This guide will walk you through setting up the MoodLog application on your local machine step by step.

## 📋 Prerequisites

Before starting, ensure you have the following installed on your machine:

### Required Software
1. **.NET 8.0 SDK** (Latest version)
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version` (should show 8.0.x)

2. **Git** (for cloning the repository)
   - Download from: https://git-scm.com/downloads
   - Verify installation: `git --version`

3. **Code Editor** (Choose one):
   - **Visual Studio 2022** (Recommended for Windows)
   - **Visual Studio Code** (Cross-platform)
   - **JetBrains Rider** (Premium option)

### Optional but Recommended
- **SQLite Browser** (for database inspection): https://sqlitebrowser.org/
- **Postman** (for API testing): https://www.postman.com/downloads/

## 🚀 Step-by-Step Installation

### Step 1: Clone the Repository

Open your terminal/command prompt and run:

```bash
git clone https://github.com/vsevolodanhelis/MoodLog.git
cd MoodLog
```

### Step 2: Verify Project Structure

Your folder structure should look like this:
```
MoodLog/
├── src/
│   ├── MoodLog.Core/           # Business logic
│   ├── MoodLog.Application/    # Services
│   ├── MoodLog.Infrastructure/ # Data access
│   └── MoodLog.Web/           # Web app
├── tests/
│   ├── MoodLog.Core.Tests/
│   └── MoodLog.Application.Tests/
├── MoodLogApp.sln
├── README.md
└── SETUP_GUIDE.md
```

### Step 3: Restore Dependencies

Navigate to the project root and restore all NuGet packages:

```bash
dotnet restore
```

This will download all required dependencies for the entire solution.

### Step 4: Build the Solution

Build the entire solution to ensure everything compiles correctly:

```bash
dotnet build
```

You should see "Build succeeded" message with no errors.

### Step 5: Run Database Migrations

The application uses SQLite with Entity Framework Core. Initialize the database:

```bash
cd src/MoodLog.Web
dotnet ef database update
```

If you don't have Entity Framework tools installed, run:
```bash
dotnet tool install --global dotnet-ef
```

### Step 6: Run the Application

Start the web application:

```bash
dotnet run
```

You should see output similar to:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5115
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 7: Access the Application

Open your web browser and navigate to:
- **Main Application**: http://localhost:5115
- **API Documentation**: http://localhost:5115/swagger

## 🎯 First-Time Setup

### Create Your First Account

1. Click "Register" on the homepage
2. Fill in your details:
   - Email address
   - Password (minimum 6 characters)
   - Confirm password
3. Click "Register" to create your account

### Seed Sample Data (Optional)

To get started with sample data:

1. Navigate to: http://localhost:5115/Seed
2. Click "Seed Sample Data"
3. This will create sample mood entries and tags

### Admin Access

To access admin features:
1. Register with email: `admin@moodlog.com`
2. The system will automatically assign admin role
3. Access admin panel via the sidebar

## 🧪 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/MoodLog.Core.Tests/
dotnet test tests/MoodLog.Application.Tests/
```

### View Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Development Workflow

### Making Changes

1. **Create a new branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** in the appropriate project:
   - **Business Logic**: `src/MoodLog.Core/`
   - **Services**: `src/MoodLog.Application/`
   - **Data Access**: `src/MoodLog.Infrastructure/`
   - **Web/API**: `src/MoodLog.Web/`

3. **Test your changes**:
   ```bash
   dotnet test
   dotnet run --project src/MoodLog.Web
   ```

4. **Commit and push**:
   ```bash
   git add .
   git commit -m "Add your feature description"
   git push origin feature/your-feature-name
   ```

### Hot Reload

The application supports hot reload for development:
- CSS changes: Automatically refreshed
- C# changes: Automatically recompiled and reloaded
- View changes: Automatically refreshed

## 📊 Application Features

### Core Functionality
- **Daily Mood Tracking**: Track mood with emoji interface
- **Analytics Dashboard**: View mood trends and insights
- **Timeline View**: See mood history over time
- **Settings Management**: Customize preferences
- **Data Export**: Export mood data as CSV/JSON

### API Endpoints
- **GET /api/moodentries**: Get mood entries
- **POST /api/moodentries**: Create new mood entry
- **GET /api/analytics**: Get analytics data
- **Full Swagger docs**: http://localhost:5115/swagger

### Design Features
- **High-Contrast Theme**: Pure black text on white backgrounds
- **Glassmorphism UI**: Semi-transparent design elements
- **Responsive Design**: Works on all device sizes
- **Purple Brand Identity**: Consistent color scheme

## 🛠️ Troubleshooting

### Common Issues

**1. Port Already in Use**
```bash
# Kill process on port 5115
netstat -ano | findstr :5115
taskkill /PID <process_id> /F
```

**2. Database Issues**
```bash
# Reset database
dotnet ef database drop --force
dotnet ef database update
```

**3. Package Restore Issues**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

**4. Build Errors**
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Getting Help

- **Check logs**: Application logs appear in the console
- **Database location**: `src/MoodLog.Web/moodlog.db`
- **Configuration**: `src/MoodLog.Web/appsettings.json`

## 📱 Browser Compatibility

**Supported Browsers**:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

**Recommended**: Chrome or Edge for best experience

## 🔒 Security Notes

- **Development Mode**: Application runs in development mode by default
- **Database**: SQLite database file is created locally
- **Authentication**: Uses ASP.NET Core Identity
- **HTTPS**: Enabled by default in development

## 📞 Support

If you encounter any issues:

1. **Check this guide** for common solutions
2. **Review error messages** in the console
3. **Check GitHub issues**: https://github.com/vsevolodanhelis/MoodLog/issues
4. **Contact the development team**

---

**Happy coding! 🎉**

*Last updated: December 2024*
