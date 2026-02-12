const statusBadge = document.getElementById('conn-status');
const alertList = document.getElementById('alert-list');

// 1. Connection nesnesini oluştur
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5072/alertHub")
    .build();

// 2. Event listener: Veri geldiğinde ne yapılacak?
connection.on("ReceiveAlert", function(alert) {
    // Ekranda "Waiting..." yazısı varsa kaldır
    const emptyState = document.querySelector('.empty-state');
    if (emptyState) emptyState.remove();

    // Yeni alert kartı oluştur
    const card = document.createElement('div');
    card.className = `alert-card ${alert.severity.toLowerCase()}`;
    
    card.innerHTML = `
        <div class="card-header">
            <span class="device-id">${alert.deviceId}</span>
            <span class="timestamp">${new Date(alert.timestamp).toLocaleTimeString()}</span>
        </div>
        <div class="card-body">
            <div class="alert-type">${alert.alertType}</div>
            <div class="values">
                Value: <strong>${alert.currentValue.toFixed(2)}</strong> 
                <span class="divider">|</span> 
                Limit: ${alert.thresholdValue}
            </div>
        </div>
        <div class="severity-tag">${alert.severity}</div>
    `;

    // En başa ekle (Yeni gelen en üstte görünsün)
    alertList.prepend(card);
});

// 3. Bağlantıyı başlat ve UI'ı güncelle
connection.start()
    .then(() => {
        statusBadge.innerText = "Connected";
        statusBadge.classList.add('connected');
        console.log("SignalR Connected! 🚀");
    })
    .catch(err => {
        statusBadge.innerText = "Connection Failed";
        statusBadge.style.background = "#f43f5e";
        console.error("Connection Error: ", err);
    });