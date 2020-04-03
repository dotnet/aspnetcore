export async function loadTimezone() : Promise<void> {
    const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const timeZoneArea = timeZone.split('/')[0];

    let arrayBuffer: ArrayBuffer;
    try {
        const request = await fetch(`zoneinfo/${timeZone}`);
        arrayBuffer = await request.arrayBuffer();
    } catch (err) {
        console.warn(`Unable to fetch time zone databse for ${timeZone}.`);
        return;
    }

    Module['FS_createPath']('/', 'zoneinfo', true, true);
    Module['FS_createPath']('/zoneinfo', timeZoneArea, true, true);

    const bytes = Uint8Array.from(new Uint8Array(arrayBuffer));
    Module['FS_createDataFile'](`/zoneinfo/${timeZone}`, null, bytes, true, true, true);
}
