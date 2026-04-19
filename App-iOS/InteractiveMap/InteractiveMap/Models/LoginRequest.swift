//
//  LoginRequest.swift
//  InteractiveMap
//
//  Created by Andrii Trybushnyi on 07.04.2025.
//


struct LoginRequest: Codable, Sendable {
    let username: String
    let password: String
}

