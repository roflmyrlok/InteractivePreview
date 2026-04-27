//
//  DevSettingsView.swift
//  InteractiveMap
//
//  Developer-facing settings screen. Currently exposes a runtime override for
//  the AWS API host used by all service calls (see APIConstants).
//

import SwiftUI

struct DevSettingsView: View {
    @Environment(\.dismiss) private var dismiss

    @State private var hostInput: String = ""
    @State private var statusMessage: String?
    @State private var statusIsError: Bool = false
    @State private var showResetConfirm: Bool = false
    @State private var activeBaseURL: String = APIConstants.baseURL
    @State private var isUsingDefault: Bool = APIConstants.isUsingDefault

    var body: some View {
        NavigationView {
            Form {
                Section(header: Text("Active API Host")) {
                    LabeledRow(label: "Base URL", value: activeBaseURL, valueIsMonospaced: true)
                    LabeledRow(
                        label: "Source",
                        value: isUsingDefault ? "Default (compiled-in)" : "Custom override",
                        valueColor: isUsingDefault ? .green : .orange
                    )
                    LabeledRow(
                        label: "Default",
                        value: APIConstants.defaultBaseURL,
                        valueIsMonospaced: true,
                        valueColor: .secondary
                    )
                }

                Section(
                    header: Text("Override Host"),
                    footer: Text(
                        "Enter a full base URL such as http://staging.example.com or http://192.168.1.10:5000. Leave blank and tap Save to clear the override and restore the default host."
                    )
                ) {
                    TextField("http://host[:port]", text: $hostInput)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled(true)
                        .keyboardType(.URL)
                        .font(.system(.body, design: .monospaced))

                    Button(action: saveHost) {
                        Label("Save", systemImage: "tray.and.arrow.down.fill")
                    }

                    Button(role: .destructive, action: { showResetConfirm = true }) {
                        Label("Reset to Default", systemImage: "arrow.uturn.backward")
                    }
                    .disabled(isUsingDefault)
                }

                if let statusMessage = statusMessage {
                    Section(header: Text("Status")) {
                        Text(statusMessage)
                            .foregroundColor(statusIsError ? .red : .green)
                            .font(.footnote)
                    }
                }

                Section(header: Text("Resolved Endpoints"), footer: Text("Service URLs are computed from the active base URL and update automatically when the host changes.")) {
                    LabeledRow(label: "Auth", value: APIConstants.authServiceURL, valueIsMonospaced: true, valueColor: .secondary)
                    LabeledRow(label: "Users", value: APIConstants.userServiceURL, valueIsMonospaced: true, valueColor: .secondary)
                    LabeledRow(label: "Locations", value: APIConstants.locationServiceURL, valueIsMonospaced: true, valueColor: .secondary)
                    LabeledRow(label: "Reviews", value: APIConstants.reviewServiceURL, valueIsMonospaced: true, valueColor: .secondary)
                }
            }
            .navigationTitle("Developer Settings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Close") { dismiss() }
                }
            }
            .onAppear {
                refreshActiveState()
                // Pre-populate the text field with whatever override is currently
                // stored so developers can tweak it in place.
                hostInput = isUsingDefault ? "" : activeBaseURL
            }
            .confirmationDialog(
                "Reset API host to default?",
                isPresented: $showResetConfirm,
                titleVisibility: .visible
            ) {
                Button("Reset", role: .destructive) {
                    APIConstants.resetToDefault()
                    hostInput = ""
                    refreshActiveState()
                    statusIsError = false
                    statusMessage = "Override cleared. Using default host."
                }
                Button("Cancel", role: .cancel) {}
            }
        }
    }

    // MARK: - Actions

    private func saveHost() {
        let success = APIConstants.setCustomBaseURL(hostInput)
        refreshActiveState()

        if success {
            statusIsError = false
            statusMessage = hostInput.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
                ? "Override cleared. Using default host."
                : "Host saved. Active base URL: \(activeBaseURL)"
        } else {
            statusIsError = true
            statusMessage = "That value isn't a valid URL. Include a scheme (http:// or https://) and a host."
        }
    }

    private func refreshActiveState() {
        activeBaseURL = APIConstants.baseURL
        isUsingDefault = APIConstants.isUsingDefault
    }
}

// MARK: - Helper view

private struct LabeledRow: View {
    let label: String
    let value: String
    var valueIsMonospaced: Bool = false
    var valueColor: Color = .primary

    var body: some View {
        HStack(alignment: .firstTextBaseline) {
            Text(label)
                .foregroundColor(.secondary)
            Spacer(minLength: 12)
            Text(value)
                .multilineTextAlignment(.trailing)
                .font(valueIsMonospaced ? .system(.footnote, design: .monospaced) : .body)
                .foregroundColor(valueColor)
                .textSelection(.enabled)
        }
    }
}

struct DevSettingsView_Previews: PreviewProvider {
    static var previews: some View {
        DevSettingsView()
    }
}
