using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MoodLog.Web.Services;

public class SecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly Dictionary<string, List<DateTime>> _rateLimitTracker;
    private readonly Dictionary<string, int> _failedAttempts;

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
        _rateLimitTracker = new Dictionary<string, List<DateTime>>();
        _failedAttempts = new Dictionary<string, int>();
    }

    public bool IsRateLimited(string identifier, int maxRequests = 100, TimeSpan? timeWindow = null)
    {
        timeWindow ??= TimeSpan.FromMinutes(15);
        var now = DateTime.UtcNow;
        var windowStart = now.Subtract(timeWindow.Value);

        lock (_rateLimitTracker)
        {
            if (!_rateLimitTracker.ContainsKey(identifier))
            {
                _rateLimitTracker[identifier] = new List<DateTime>();
            }

            var requests = _rateLimitTracker[identifier];
            
            // Remove old requests outside the time window
            requests.RemoveAll(r => r < windowStart);
            
            if (requests.Count >= maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for identifier: {Identifier}", identifier);
                return true;
            }

            requests.Add(now);
            return false;
        }
    }

    public bool IsAccountLocked(string identifier, int maxFailedAttempts = 5)
    {
        lock (_failedAttempts)
        {
            return _failedAttempts.ContainsKey(identifier) && 
                   _failedAttempts[identifier] >= maxFailedAttempts;
        }
    }

    public void RecordFailedAttempt(string identifier)
    {
        lock (_failedAttempts)
        {
            if (!_failedAttempts.ContainsKey(identifier))
            {
                _failedAttempts[identifier] = 0;
            }
            
            _failedAttempts[identifier]++;
            _logger.LogWarning("Failed attempt recorded for identifier: {Identifier}. Total: {Count}", 
                identifier, _failedAttempts[identifier]);
        }
    }

    public void ResetFailedAttempts(string identifier)
    {
        lock (_failedAttempts)
        {
            _failedAttempts.Remove(identifier);
        }
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Remove potentially dangerous characters
        var sanitized = input
            .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
            .Replace("</script>", "&lt;/script&gt;", StringComparison.OrdinalIgnoreCase)
            .Replace("javascript:", "javascript&#58;", StringComparison.OrdinalIgnoreCase)
            .Replace("vbscript:", "vbscript&#58;", StringComparison.OrdinalIgnoreCase)
            .Replace("onload=", "onload&#61;", StringComparison.OrdinalIgnoreCase)
            .Replace("onerror=", "onerror&#61;", StringComparison.OrdinalIgnoreCase);

        // Remove excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        return sanitized;
    }

    public bool ContainsSuspiciousContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        var suspiciousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"eval\s*\(",
            @"document\.cookie",
            @"window\.location"
        };

        return suspiciousPatterns.Any(pattern => 
            Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
    }

    public string HashSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data)) return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email) && email.Length <= 254;
        }
        catch
        {
            return false;
        }
    }

    public bool IsStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        // At least 8 characters, contains uppercase, lowercase, digit, and special character
        var hasMinLength = password.Length >= 8;
        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        return hasMinLength && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }

    public SecurityAuditResult PerformSecurityAudit(string userInput, string userIdentifier)
    {
        var result = new SecurityAuditResult
        {
            IsSecure = true,
            Issues = new List<string>()
        };

        // Check for suspicious content
        if (ContainsSuspiciousContent(userInput))
        {
            result.IsSecure = false;
            result.Issues.Add("Input contains potentially malicious content");
            _logger.LogWarning("Suspicious content detected from user: {UserIdentifier}", userIdentifier);
        }

        // Check rate limiting
        if (IsRateLimited(userIdentifier))
        {
            result.IsSecure = false;
            result.Issues.Add("Rate limit exceeded");
        }

        // Check account lockout
        if (IsAccountLocked(userIdentifier))
        {
            result.IsSecure = false;
            result.Issues.Add("Account is temporarily locked due to failed attempts");
        }

        return result;
    }
}

public class SecurityAuditResult
{
    public bool IsSecure { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;
}
