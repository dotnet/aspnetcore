import { readInt32LE } from "../../BinaryDecoder";
import { decodeUtf8 } from "../../Utf8Decoder";

export function loadTimezoneData(arrayBuffer: ArrayBuffer) {
    let remainingData = new Uint8Array(arrayBuffer);

    // The timezone file is generated by https://github.com/dotnet/blazor/tree/master/src/TimeZoneData.
    // The file format of the TZ file look like so
    //
    // [4 - byte length of manifest]
    // [json manifest]
    // [data bytes]
    //
    // The json manifest is an array that looks like so:
    //
    // [...["America/Fort_Nelson",2249],["America/Glace_Bay",2206]..]
    //
    // where the first token in each array is the relative path of the file on disk, and the second is the
    // length of the file. The starting offset of a file can be calculated using the lengths of all files
    // that appear prior to it.
    const manifestSize = readInt32LE(remainingData, 0);
    remainingData = remainingData.slice(4);
    const manifestContent = decodeUtf8(remainingData.slice(0, manifestSize));
    const manifest = JSON.parse(manifestContent) as ManifestEntry[];
    remainingData = remainingData.slice(manifestSize);

    // Create the folder structure
    // /zoneinfo
    // /zoneinfo/Africa
    // /zoneinfo/Asia
    // ..
    Module['FS_createPath']('/', 'zoneinfo', true, true);
    new Set(manifest.map(m => m[0].split('/')![0])).forEach(folder =>
      Module['FS_createPath']('/zoneinfo', folder, true, true));

    for (const [name, length] of manifest) {
      const bytes = remainingData.slice(0, length);
      Module['FS_createDataFile'](`/zoneinfo/${name}`, null, bytes, true, true, true);
      remainingData = remainingData.slice(length);
    }
  }

  type ManifestEntry = [string, number];