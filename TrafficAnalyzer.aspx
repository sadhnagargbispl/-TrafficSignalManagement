<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TrafficAnalyzer.aspx.vb" Inherits="TrafficAnalyzer" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Traffic Analyzer</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f5f6fa;
            margin: 0;
            padding: 20px;
        }

        h2 {
            text-align: center;
            color: #333;
        }

        #grid {
            display: flex;
            justify-content: center;
            flex-wrap: wrap;
            margin-top: 20px;
        }

        .panel {
            background-color: #fff;
            display: inline-block;
            padding: 15px;
            margin: 10px;
            border-radius: 10px;
            width: 130px;
            text-align: center;
            box-shadow: 0 0 10px rgba(0,0,0,0.15);
            transition: transform 0.2s;
        }

        .panel:hover {
            transform: scale(1.05);
        }

        .sig {
            padding: 12px 0;
            margin: 10px 0;
            border-radius: 8px;
            font-weight: bold;
            color: #fff;
        }

        .green { background: #28a745; }
        .red { background: #dc3545; }

        button {
            margin-top: 6px;
            padding: 8px;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            background-color: #007bff;
            color: #fff;
            transition: background-color 0.3s;
        }

        button:hover {
            background-color: #0056b3;
        }

        #status {
            text-align: center;
            margin-top: 15px;
            font-size: 16px;
        }

        #status strong {
            margin-right: 15px;
        }

        a {
            text-decoration: none;
            color: #007bff;
            margin-left: 15px;
        }

        a:hover {
            text-decoration: underline;
        }

        @media(max-width:600px) {
            #grid { flex-direction: column; align-items: center; }
            .panel { width: 80%; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h2>Traffic Analyzer</h2>

        <div id="status">
            <strong>Time:</strong> <span id="lblTime">0</span>
            <strong>Remaining:</strong> <span id="lblRemain">0</span>
            <strong>Current Open:</strong> <span id="lblCurrent">-</span>
            <strong>Next Open:</strong> <span id="lblNext">-</span>
            <strong>Direction:</strong> <span id="lblDirection"><%= DirectionText %></span>
            <a href="GlobalConfiguration.aspx">Settings</a>
        </div>

        <div id="grid">
            <div class="panel">
                <div>A</div>
                <div id="sigA" class="sig red">Closed</div>
                <button type="button" onclick="ambulance('A')">AA (Ambulance)</button>
            </div>
            <div class="panel">
                <div>B</div>
                <div id="sigB" class="sig red">Closed</div>
                <button type="button" onclick="ambulance('B')">AB (Ambulance)</button>
            </div>
            <div class="panel">
                <div>C</div>
                <div id="sigC" class="sig red">Closed</div>
                <button type="button" onclick="ambulance('C')">AC (Ambulance)</button>
            </div>
            <div class="panel">
                <div>D</div>
                <div id="sigD" class="sig red">Closed</div>
                <button type="button" onclick="ambulance('D')">AD (Ambulance)</button>
            </div>
        </div>

        <script type="text/javascript">
            var intervalSec = <%= IntervalSeconds %>;
            var direction = "<%= DirectionText %>";
            var sequences = {
                "Clockwise": ["A","B","C","D"],
                "Anti Clock wise": ["A","D","C","B"],
                "Up & Down": ["A","C"],
                "Left & Right": ["B","D"]
            };
            var seq = sequences[direction] || ["A","B","C","D"];
            var idx = 0;
            var remaining = intervalSec;
            var timerId = null;

            function setSignal(openId) {
                ["A","B","C","D"].forEach(function(s){
                    var el = document.getElementById("sig"+s);
                    if (!el) return;
                    if (s === openId) { el.className="sig green"; el.textContent="Open"; }
                    else { el.className="sig red"; el.textContent="Closed"; }
                });
                document.getElementById("lblCurrent").textContent = openId;
                var next = seq[(seq.indexOf(openId)+1) % seq.length];
                document.getElementById("lblNext").textContent = next;
            }

            function renderHeader(){
                var elapsed = intervalSec - remaining;
                document.getElementById("lblTime").textContent = elapsed;
                document.getElementById("lblRemain").textContent = remaining;
            }

            function advanceTo(index){
                idx = index;
                remaining = intervalSec;
                setSignal(seq[idx]);
                renderHeader();
            }

            function nextStep(){
                idx = (idx + 1) % seq.length;
                remaining = intervalSec;
                setSignal(seq[idx]);
            }

            function tick(){
                renderHeader();
                remaining--;
                if (remaining < 0){
                    nextStep();
                    renderHeader();
                    remaining = intervalSec - 1;
                }
            }

            function ambulance(which){
                var targetIndex = seq.indexOf(which);
                if (targetIndex === -1) return;
                advanceTo(targetIndex);
            }

            window.ambulance = ambulance;
            window.onload = function(){
                document.getElementById("lblDirection").textContent = direction;
                advanceTo(0);
                if (timerId) clearInterval(timerId);
                timerId = setInterval(tick, 1000);
            };
        </script>
    </form>
</body>
</html>
