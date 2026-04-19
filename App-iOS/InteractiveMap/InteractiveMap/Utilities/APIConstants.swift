//
//  APIConstants.swift
//  InteractiveMap
//
//  Created by Andrii Trybushnyi on 07.04.2025.
//

import Foundation

/// Runtime-configurable API configuration.
///
/// The AWS host used to be hard-coded. It is now resolved dynamically so the
/// in-app Developer Settings screen can switch between the default production
/// host and alternative (staging / local / ad-hoc) environments without a
/// rebuild. All service URL helpers are computed from `baseURL`, so any
/// runtime change takes effect immediately for the next network request.
///
/// NOTE: The backend is currently HTTP-only (no TLS). The Info.plist carries
/// an `NSAllowsArbitraryLoads` ATS exception so plain-HTTP traffic is allowed.
/// Do NOT switch the default scheme to HTTPS — the backend does not terminate
/// TLS and the connection will fail.
struct APIConstants: Sendable {
    // MARK: - Defaults

    /// Compiled-in default (production) AWS host. Used whenever no override
    /// has been stored via Developer Settings. HTTP on purpose — see the
    /// type-level comment above.
    static let defaultBaseURL = "http://ec2-63-177-81-123.eu-central-1.compute.amazonaws.com"

    /// `UserDefaults` key holding the developer-provided override host.
    static let customBaseURLKey = "custom_api_base_url"

    /// Notification posted whenever the active base URL changes. Views that
    /// cache URL-derived state can observe this to refresh.
    static let baseURLDidChangeNotification = Notification.Name("APIConstants.baseURLDidChange")

    // MARK: - Active configuration

    /// Resolved base URL. Prefers the developer-provided override, otherwise
    /// falls back to `defaultBaseURL`. Any trailing slash is stripped to keep
    /// service URL composition predictable.
    static var baseURL: String {
        let stored = (UserDefaults.standard.string(forKey: customBaseURLKey) ?? "")
            .trimmingCharacters(in: .whitespacesAndNewlines)

        let resolved = stored.isEmpty ? defaultBaseURL : stored
        return resolved.hasSuffix("/") ? String(resolved.dropLast()) : resolved
    }

    /// True when no override is active and the app is talking to `defaultBaseURL`.
    static var isUsingDefault: Bool {
        let stored = (UserDefaults.standard.string(forKey: customBaseURLKey) ?? "")
            .trimmingCharacters(in: .whitespacesAndNewlines)
        return stored.isEmpty
    }

    // MARK: - Service endpoints

    // Computed so they pick up any runtime override immediately.
    static var userServiceURL: String { "\(baseURL)/api/users" }
    static var authServiceURL: String { "\(baseURL)/api/auth" }
    static var locationServiceURL: String { "\(baseURL)/api/locations" }
    static var reviewServiceURL: String { "\(baseURL)/api/reviews" }

    // MARK: - Mutation API (used by Developer Settings)

    /// Stores a custom base URL. Passing `nil` or an empty string clears the
    /// override and restores the default host. Returns `false` if the input
    /// string is not a parseable URL (and no change is made).
    @discardableResult
    static func setCustomBaseURL(_ url: String?) -> Bool {
        let trimmed = (url ?? "").trimmingCharacters(in: .whitespacesAndNewlines)

        if trimmed.isEmpty {
            UserDefaults.standard.removeObject(forKey: customBaseURLKey)
            NotificationCenter.default.post(name: baseURLDidChangeNotification, object: nil)
            return true
        }

        // Must parse as a URL and carry a scheme + host to be usable.
        guard let parsed = URL(string: trimmed),
              let scheme = parsed.scheme, !scheme.isEmpty,
              let host = parsed.host, !host.isEmpty else {
            return false
        }

        UserDefaults.standard.set(trimmed, forKey: customBaseURLKey)
        NotificationCenter.default.post(name: baseURLDidChangeNotification, object: nil)
        return true
    }

    /// Convenience: drops any custom override and returns to `defaultBaseURL`.
    static func resetToDefault() {
        UserDefaults.standard.removeObject(forKey: customBaseURLKey)
        NotificationCenter.default.post(name: baseURLDidChangeNotification, object: nil)
    }
}
