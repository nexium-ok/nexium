const http = require("http");

const server = http.createServer((req, res) => {
  res.writeHead(503, {
    "Content-Type": "text/html; charset=utf-8"
  });

  res.end(`
    <!DOCTYPE html>
    <html>
      <head>
        <title>We'll be back soon</title>
        <style>
          body {
            margin: 0;
            height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #0f1115;
            color: white;
            font-family: Arial, sans-serif;
            text-align: center;
          }

          h1 {
            font-size: 42px;
            margin-bottom: 10px;
          }

          p {
            color: #a0a0a0;
            font-size: 18px;
          }
        </style>
      </head>
      <body>
        <div>
          <h1>We'll be back soon</h1>
          <p>Nexrev is temporarily unavailable.</p>
        </div>
      </body>
    </html>
  `);
});

server.listen(5000, () => {
    console.log("Server running on port 3000");
});