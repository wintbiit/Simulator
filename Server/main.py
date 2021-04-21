import os
import asyncio
import websockets
import subprocess

executable_path = "Build/Simulator.exe"
server_socket = None
client_sockets = set()
connect_count = 0


async def forward(websocket, path):
    global connect_count
    global server_socket
    connect_count += 1
    if websocket.remote_address[0] == "127.0.0.1" and connect_count == 1:
        try:
            server_socket = websocket
            await server_socket.send("Server")
            async for message in websocket:
                if len(client_sockets) > 0:
                    await asyncio.wait([client.send(message) for client in client_sockets])
        finally:
            server_socket = None
    else:
        client_sockets.add(websocket)
        try:
            async for message in websocket:
                if server_socket is not None:
                    await server_socket.send(message)
        finally:
            client_sockets.remove(websocket)

if __name__ == "__main__":
    if os.path.exists(executable_path):
        subprocess.Popen(os.path.join(os.getcwd(), executable_path))
    asyncio.get_event_loop().run_until_complete(
        websockets.serve(forward, "localhost", 8765)
    )
    asyncio.get_event_loop().run_forever()
