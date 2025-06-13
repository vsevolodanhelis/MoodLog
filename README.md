# MoodLog - Daily Mood Tracker

A comprehensive mental wellness platform built with ASP.NET Core, featuring AI-powered insights, gamification systems, and sophisticated glassmorphism design.

## 🌟 Features

### Core Functionality
- **Daily Mood Tracking** - Track your mood with emoji-based interface
- **Timeline View** - Visual timeline of your mood history
- **Analytics Dashboard** - Comprehensive mood analytics with charts and insights
- **Settings Management** - Customizable preferences and data export

### Design System
- **High-Contrast White Theme** - Maximum text readability with pure black text (#000000)
- **Glassmorphism UI** - Sophisticated semi-transparent design elements
- **Purple Brand Identity** - Consistent brand colors throughout the application
- **Responsive Design** - Works seamlessly across all device sizes

### Technical Features
- **REST API** - Complete API endpoints for all functionality
- **Event-Driven Architecture** - C# delegates for audit logging and notifications
- **Unit Testing** - 82 comprehensive unit tests with full business logic coverage
- **Swagger Documentation** - Interactive API documentation
- **Identity Management** - Multi-user authentication with role-based access

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- SQLite (included)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/vsevolodanhelis/MoodLog.git
   cd MoodLog
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/MoodLog.Web
   ```

4. **Access the application**
   - Open your browser and navigate to `http://localhost:5115`
   - Register a new account or use the demo data

## 🏗️ Project Structure

```
MoodLog/
├── src/
│   ├── MoodLog.Core/           # Core business logic and entities
│   ├── MoodLog.Infrastructure/ # Data access and external services
│   └── MoodLog.Web/           # Web application and API
├── tests/
│   └── MoodLog.Tests/         # Unit tests
└── docs/                      # Documentation
```

## 🎨 Design System

### Color Palette
- **Text Colors**: Pure black (#000000) for maximum readability
- **Brand Colors**: Purple gradient (#7B61FF to #B8A1FF)
- **Backgrounds**: Pure white (#FFFFFF) with glassmorphism effects
- **Accents**: High-contrast system for optimal accessibility

### Typography
- **Font Family**: Inter, Segoe UI, system fonts
- **Hierarchy**: Clear distinction through weight and color
- **Accessibility**: WCAG AA compliant contrast ratios

## 🧪 Testing

Run the comprehensive test suite:

```bash
dotnet test
```

**Test Coverage:**
- 82 unit tests
- Full business logic coverage
- Event system testing
- API endpoint validation

## 📊 API Documentation

The application includes Swagger/OpenAPI documentation available at:
- Development: `http://localhost:5115/swagger`

## 🔧 Configuration

### Database
- SQLite database (default)
- Entity Framework Core migrations
- Automatic database initialization

### Authentication
- ASP.NET Core Identity
- Role-based authorization
- Admin user management

## 🎯 Roadmap

### Planned Features
- **AI-Powered Insights** - ML.NET integration for mood predictions
- **OpenAI Integration** - Intelligent mood summaries and recommendations
- **Gamification System** - Achievement badges and progress tracking
- **Clinical Reporting** - Professional mood reports for healthcare providers
- **PWA Support** - Progressive Web App capabilities
- **Therapist Portal** - Collaboration tools for mental health professionals

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ASP.NET Core and Entity Framework
- UI inspired by modern glassmorphism design principles
- Icons from Font Awesome
- Charts powered by Chart.js

## 📞 Support

For support, please open an issue on GitHub or contact the development team.

---

**MoodLog** - Empowering mental wellness through technology 🧠💜
