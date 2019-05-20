var clockConnection = new signalR.HubConnectionBuilder().withUrl("/streamingtime").build();

clockConnection.start().then(() => {
    clockConnection
        .stream('ServerTimer')
            .subscribe({
                next: (serverTime) => {
                    document.getElementById('serverTime').innerText = serverTime;
                },
                complete: () => {
                    console.log('complete!');
                },
                error: (err) => {
                    console.log(err);
                }
            });
    });