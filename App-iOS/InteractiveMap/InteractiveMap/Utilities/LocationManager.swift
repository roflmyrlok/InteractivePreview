// App-iOS/InteractiveMap/InteractiveMap/Utilities/LocationManager.swift

import Foundation
import CoreLocation
import MapKit
import SwiftUI

@MainActor
final class LocationManager: NSObject, ObservableObject {
    private let locationManager = CLLocationManager()

    @Published var location: CLLocation?
    @Published var authorizationStatus: CLAuthorizationStatus = .notDetermined
    @Published var region = MKCoordinateRegion(
        center: CLLocationCoordinate2D(latitude: 50.4501, longitude: 30.5234),
        span: MKCoordinateSpan(latitudeDelta: 0.1, longitudeDelta: 0.1)
    )

    // Simplified approach - let's just provide a method to get the position
    func getCameraPosition() -> MapCameraPosition {
        return MapCameraPosition.region(self.region)
    }

    // And a method to update from a position
    func updateFromCameraPosition(_ position: MapCameraPosition) {
        // For now, we'll skip this as it's complicated to extract the region
        // We'll use a different binding approach in the Map view
    }

    private var shouldUpdateRegion = true

    override init() {
        super.init()
        locationManager.delegate = self
        locationManager.desiredAccuracy = kCLLocationAccuracyBest
        locationManager.requestWhenInUseAuthorization()
    }

    func requestLocation() {
        locationManager.requestLocation()
    }

    func updateRegion(location: CLLocation) {
        self.shouldUpdateRegion = true
        self.region = MKCoordinateRegion(
            center: location.coordinate,
            span: MKCoordinateSpan(latitudeDelta: 0.05, longitudeDelta: 0.05)
        )
    }

    func stopUpdatingRegion() {
        shouldUpdateRegion = false
    }

    func userInteractionBegan() {
        shouldUpdateRegion = false
        locationManager.stopUpdatingLocation()
    }
}

extension LocationManager: CLLocationManagerDelegate {
    nonisolated func locationManager(_ manager: CLLocationManager, didUpdateLocations locations: [CLLocation]) {
        guard let location = locations.last else { return }
        Task { @MainActor in
            self.location = location

            if self.shouldUpdateRegion {
                self.updateRegion(location: location)
                self.shouldUpdateRegion = false
            }
        }
    }

    nonisolated func locationManager(_ manager: CLLocationManager, didFailWithError error: Error) {
        print("Location manager error: \(error.localizedDescription)")
    }

    nonisolated func locationManagerDidChangeAuthorization(_ manager: CLLocationManager) {
        Task { @MainActor in
            self.authorizationStatus = manager.authorizationStatus

            switch manager.authorizationStatus {
            case .authorizedWhenInUse, .authorizedAlways:
                self.locationManager.requestLocation()
            case .notDetermined:
                self.locationManager.requestWhenInUseAuthorization()
            default:
                break
            }
        }
    }
}
