import ArgumentParser
import Foundation
import AppKit

@main
struct scalusmac: ParsableCommand {
    @Flag(name: .shortAndLong, help: "Register the SCALUS handler.")
    var register = false;
    
    @Flag(name: .shortAndLong, help: "Unregister the the SCALUS handler.")
    var unregister = false;
    
    @Argument(help: "The URL scheme for which the SCALUS handler is to be set.")
    var URLScheme: String
    
    mutating func run() throws {
        if register || unregister {
            let handler = register ? "com.oneidentity.scalus.macos" : String()
            let result = LSSetDefaultHandlerForURLScheme(URLScheme as CFString, handler as CFString)
            print(result)
        } else {
            let result = LSCopyDefaultHandlerForURLScheme(URLScheme as CFString)
            print(result?.takeRetainedValue() as Any)
        }
    }
}
