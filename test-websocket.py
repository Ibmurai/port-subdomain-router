#!/usr/bin/env python3
"""
Simple test script for the WebSocket proxy
This script tests the port extraction and basic WebSocket functionality
"""

import asyncio
import sys
import re

try:
    import websockets
    WEBSOCKETS_AVAILABLE = True
except ImportError:
    WEBSOCKETS_AVAILABLE = False

async def test_websocket_connection(host, port):
    """Test WebSocket connection to the proxy"""
    uri = f"ws://{host}:{port}"
    print(f"Testing connection to {uri}")
    
    try:
        async with websockets.connect(uri) as websocket:
            print("PASS: WebSocket connection established")
            
            # Send a test message
            await websocket.send(b"test message")
            print("PASS: Test message sent")
            
            # Wait for response (if any)
            try:
                response = await asyncio.wait_for(websocket.recv(), timeout=2.0)
                print(f"PASS: Received response: {response}")
            except asyncio.TimeoutError:
                print("INFO: No response received (expected for proxy)")
                
    except Exception as e:
        print(f"FAIL: Connection failed: {e}")
        return False
    
    return True

def test_port_extraction():
    """Test the port extraction logic"""
    test_cases = [
        ("12345.vnc.example.com", 12345),
        ("40000.vnc.example.com", 40000),
        ("42000.vnc.example.com", 42000),
        ("9999.vnc.example.com", None),  # Too low
        ("42001.vnc.example.com", None), # Too high
        ("invalid.vnc.example.com", None), # Invalid format
    ]
    
    print("Testing port extraction:")
    for host, expected_port in test_cases:
        # Simulate the regex from the C# code
        match = re.match(r'^([1-3][0-9]{4}|40[0-9]{3}|41[0-9]{3}|42000)\.', host)
        if match:
            port = int(match.group(1))
            if 10000 <= port <= 42000:
                extracted_port = port
            else:
                extracted_port = None
        else:
            extracted_port = None
            
        status = "PASS" if extracted_port == expected_port else "FAIL"
        print(f"  {status} {host} -> {extracted_port} (expected: {expected_port})")

def main():
    print("Port Subdomain Router - Test Script")
    print("=" * 40)
    
    # Test port extraction logic
    test_port_extraction()
    print()
    
    # Test WebSocket connection if host and port provided
    if len(sys.argv) >= 3:
        if not WEBSOCKETS_AVAILABLE:
            print("WARNING: websockets module not available. Install with: pip install websockets")
            return
        host = sys.argv[1]
        port = int(sys.argv[2])
        asyncio.run(test_websocket_connection(host, port))
    else:
        print("To test WebSocket connection, run:")
        print("  python3 test-websocket.py <host> <port>")
        print("Example:")
        print("  python3 test-websocket.py localhost 8443")
        if not WEBSOCKETS_AVAILABLE:
            print("\nNote: websockets module not available. Install with: pip install websockets")

if __name__ == "__main__":
    main()
