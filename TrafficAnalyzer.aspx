<%@ Page Language="VB" AutoEventWireup="false" CodeFile="TrafficAnalyzer.aspx.vb" Inherits="TrafficAnalyzer" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
<style>
  .panel { display:inline-block; padding:10px; margin:6px; border:1px solid #ccc; border-radius:8px; width:110px; text-align:center; }
  .sig { padding:10px; margin-top:6px; border-radius:6px; }
  .green { background:#6f6; }
  .red { background:#f66; }
  button { margin:4px; }
</style>
</head>
<body>
<form id="form1" runat="server">
  <h2>Traffic Analyzer</h2>

  <div>
    <strong>Time:</strong> <span id="lblTime">0</span> &nbsp; 
    <strong>Remaining:</strong> <span id="lblRemain">0</span> &nbsp; 
    <strong>Current Open:</strong> <span id="lblCurrent">-</span> &nbsp;
    <strong>Next Open:</strong> <span id="lblNext">-</span> &nbsp;
    <strong>Direction:</strong> <span id="lblDirection" ><%= DirectionText %></span>
    &nbsp;&nbsp; <a href="GlobalConfiguration.aspx">Settings</a>
  </div>

  <hr />

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
debugger ;
  // ----- CONFIG coming from code-behind -----
  var intervalSec = <%= IntervalSeconds %>;      // e.g., 15
  var direction = "<%= DirectionText %>";        // "Clockwise" | "Anti Clock wise" | "Up & Down" | "Left & Right"

  // ----- SEQUENCES exactly as spec -----
  var sequences = {
    "Clockwise": ["A","B","C","D"],
    "Anti Clock wise": ["A","D","C","B"],
    "Up & Down": ["A","C"],
    "Left & Right": ["B","D"]
  };
  var seq = sequences[direction] || ["A","B","C","D"];

  // ----- STATE -----
  var idx = 0;                 // current index in seq
  var remaining = intervalSec; // shows Interval -> 0
  var timerId = null;

  // Helper: set panel colors & labels
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

  // Tick exactly like PDF: Remaining counts down, Time counts up
  function renderHeader(){
    var elapsed = intervalSec - remaining; // 0..Interval
    document.getElementById("lblTime").textContent = elapsed;
    document.getElementById("lblRemain").textContent = remaining;
  }

  function advanceTo(index){
    idx = index;
    remaining = intervalSec;   // reset as soon as we switch
    setSignal(seq[idx]);
    renderHeader();
  }

  function nextStep(){
    idx = (idx + 1) % seq.length;
    remaining = intervalSec;
    setSignal(seq[idx]);
  }

  function tick(){
    // first render current values
    renderHeader();

    // then count down; when it hits 0, show 0 for this tick, next tick will advance
    remaining--;

    if (remaining < 0){
      nextStep();
      renderHeader();  // reflect the immediate switch
      // after switching, set remaining to Interval-1 so user sees Interval next second
      remaining = intervalSec - 1;
    }
  }

  // Ambulance: open immediately, reset timer, continue normal order after
  function ambulance(which){
    var targetIndex = seq.indexOf(which);
    if (targetIndex === -1) return;
    advanceTo(targetIndex);   // instant open + full interval reset
  }

  // Expose for buttons
  window.ambulance = ambulance;

  window.onload = function(){
    document.getElementById("lblDirection").textContent = direction;
    // Initial paint
    advanceTo(0);
    // Start timer
    if (timerId) clearInterval(timerId);
    timerId = setInterval(tick, 1000);
  };
</script>

</form>
</body>
</html>
