//Hey guys, Metano here, i made this page for the maintenance instead of l.js because i tought it looked more fitting.

const http = require("http");

const server = http.createServer((req, res) => {
  res.writeHead(503, {
    "Content-Type": "text/html; charset=utf-8"
  });

  res.end(`
    <html><head>
    <title>NEXREV Maintenance</title>
    <style type="text/css">         
        html {
             height: 100%;
         }
		body {
			background-color: #000;
			background-image: url("http://images.rbxcdn.com/img_ohnoes.jpg"); /* /images/ErrorPages/img_ohnoes.jpg */
			background-size: contain;
			background-repeat: no-repeat;
			background-position: center;
			height: auto;
			font-family: 'Source Sans Pro', sans-serif;
			font-size: 18px;
			line-height: 24px;
			text-align: center;
			margin: 0;
            overflow: hidden;
            min-width: 320px;
		}
		
		@media screen and (max-width: 991px){
			body {
			    background-size: cover;
			}
		}
		.header {
			padding: 20px 0;
		}
		
		.header img {
			width: 320px;
			margin: 0 auto;
		}

        @media screen and (max-width: 768px) {
            .header img {
                width: 256px;
            }
        }

        @media screen and (max-width: 480px) {
            body {
                background-position: 39% 50%;
            }

            .header img {
                width: 150px;
            }
        }
		.notification {
			width: auto;
			height: auto;
			padding: 12px 20px;
			margin: 0;
			background-color: #f68802;
			color: #fff;
		}

        
        @media screen and (max-width: 479px) {
            .notification {
                font-size: 14px;
                line-height: 20px;
            }
        }
	</style>
</head>
<body>
<div class="header">
    <!-- image source: /images/ErrorPages/logo_ROBLOX.png -->
    <img src="http://nexrev.org/img/Nexrevlogo.png" alt="Nexrev">
</div>
    <div class="content">
        <p class="notification">
            NEXREV is undergoing scheduled maintenance
        </p>
    </div>

    <script type="text/javascript">
        window.window.setTimeout("window.location = 'http://nexrev.org/'", 30000);
    </script>


</body></html>
  `);
});

server.listen(5000, () => {
    console.log("Server running on port 3000");
});