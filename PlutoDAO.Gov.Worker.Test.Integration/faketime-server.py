import json
from http.server import *


class FaketimeServer(BaseHTTPRequestHandler):
    print("FAKETIME SERVER RUNNING")

    def do_POST(self):
        content_len = int(self.headers.get('Content-Length'))
        content = self.rfile.read(content_len)
        json_request = json.loads(content)
        file = open('/etc/faketimerc', 'w+')
        file.write(json_request["FAKETIME"])
        file.close()
        self.send_response(200)
        self.send_header('Content-type', 'text/html')
        self.end_headers()


port = HTTPServer(('', 5555), FaketimeServer)
port.serve_forever()
