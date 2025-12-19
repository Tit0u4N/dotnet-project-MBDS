// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/online")
    .withAutomaticReconnect()
    .build();

// Handle incoming messages
connection.on("ReceiveMessage", function (message) {
    console.log("SignalR message:", message);
});

// Store local copy of players to update time-ago strings periodically
let currentPlayers = [];

// Handle receiving the player list
connection.on("ReceivePlayerList", function (players) {
    console.log("Received player list:", players);
    currentPlayers = players; // Update local copy
    updatePlayersList(players);
    updateOnlineCounter(players.length);
});

// Update time-ago strings every minute
setInterval(function () {
    if (currentPlayers.length > 0) {
        updatePlayersList(currentPlayers);
    }
}, 60000);

// Handle reconnection
connection.onreconnecting(function () {
    console.log("SignalR reconnecting...");
    updateConnectionStatus("reconnecting");
});

connection.onreconnected(function () {
    console.log("SignalR reconnected");
    updateConnectionStatus("connected");
    // Request the current player list after reconnection
    connection.invoke("GetOnlinePlayers").catch(function (err) {
        console.error("Error getting players:", err.toString());
    });
});

connection.onclose(function () {
    console.log("SignalR connection closed");
    updateConnectionStatus("disconnected");
});

// Start the connection
connection.start().then(function () {
    console.log("SignalR connected");
    updateConnectionStatus("connected");

    // Send heartbeat to register connection
    sendHeartbeat();

    // Request initial player list
    connection.invoke("GetOnlinePlayers").catch(function (err) {
        console.error("Error getting players:", err.toString());
    });

    // Send periodic heartbeat
    setInterval(function () {
        sendHeartbeat();
    }, 30000); // Every 30 seconds
}).catch(function (err) {
    console.error("SignalR connection error:", err.toString());
    updateConnectionStatus("error");
});

function sendHeartbeat() {
    connection.invoke("SendMessage").catch(function (err) {
        console.error("Heartbeat error:", err.toString());
    });
}

function updatePlayersList(players) {
    const container = document.getElementById("players-container");
    if (!container) return;

    const playerCount = document.getElementById("player-count");
    if (playerCount) {
        playerCount.textContent = players.length;
    }

    // Create new player cards with Bootstrap classes
    const playersHtml = players.map(function (player) {
        const connectedTime = new Date(player.connectedAt);
        const now = new Date();
        const diffMs = now - connectedTime;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);

        let timeAgo;
        if (diffHours > 0) {
            timeAgo = diffHours + "h " + (diffMins % 60) + "m";
        } else if (diffMins > 0) {
            timeAgo = diffMins + " min";
        } else {
            timeAgo = "Just now";
        }

        return `
            <div class="col">
                <div class="card player-card h-100">
                    <div class="card-body d-flex align-items-center gap-3">
                        <div class="player-avatar position-relative">
                            <div class="avatar-circle">${player.userName.charAt(0).toUpperCase()}</div>
                            <span class="online-dot"></span>
                        </div>
                        <div class="player-info flex-grow-1">
                            <h6 class="card-title mb-1 text-truncate">${escapeHtml(player.userName)}</h6>
                            <small class="text-muted">
                                <span class="text-success">●</span> Online for ${timeAgo}
                            </small>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join("");

    if (players.length === 0) {
        container.innerHTML = `
            <div class="col-12">
                <div class="alert alert-info text-center">
                    No players online at the moment.
                </div>
            </div>
        `;
    } else {
        container.innerHTML = playersHtml;
    }
}

function updateOnlineCounter(count) {
    const counter = document.getElementById("online-counter");
    if (counter) {
        counter.textContent = count;
    }

    // Update navbar badge if exists
    const navBadge = document.getElementById("nav-online-badge");
    if (navBadge) {
        navBadge.textContent = count;
    }
}

function updateConnectionStatus(status) {
    const statusElement = document.getElementById("connection-status");
    if (!statusElement) return;

    statusElement.className = "connection-status " + status;

    switch (status) {
        case "connected":
            statusElement.innerHTML = '<span class="status-icon">●</span> Connected';
            break;
        case "reconnecting":
            statusElement.innerHTML = '<span class="status-icon">◐</span> Reconnecting...';
            break;
        case "disconnected":
            statusElement.innerHTML = '<span class="status-icon">○</span> Disconnected';
            break;
        case "error":
            statusElement.innerHTML = '<span class="status-icon">✕</span> Connection Error';
            break;
    }
}

function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}
