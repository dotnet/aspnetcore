import { existsSync, mkdirSync, readdirSync, lstatSync, copyFileSync, rm } from 'fs';
import { join } from 'path';

function copyFolderSync(source, destination) {
    if (!existsSync(destination)) {
        mkdirSync(destination, { recursive: true });
    }

    const items = readdirSync(source);

    items.forEach(item => {
        const sourcePath = join(source, item);
        const destinationPath = join(destination, item);

        if (lstatSync(sourcePath).isDirectory()) {
            copyFolderSync(sourcePath, destinationPath);
        } else {
            copyFileSync(sourcePath, destinationPath);
        }
    });
}

const [,, sourceFolder, destinationFolder] = process.argv;

if (!sourceFolder || !destinationFolder) {
    console.error('Please provide both source and destination paths.');
    process.exit(1);
}

// If the destination folder exists, delete the folder and its contents
if (existsSync(destinationFolder)) {
    console.log(`Destination folder '${destinationFolder}' already exists. Deleting it...`);
    rm(destinationFolder, { recursive: true }, (err) => {
        if (err) {
            console.error('Error deleting destination folder:', err);
            process.exit(1);
        }
        copyFolderSync(sourceFolder, destinationFolder);
        console.log('Folder copied successfully!');
    });
}else{
    copyFolderSync(sourceFolder, destinationFolder);
    console.log('Folder copied successfully!');
}
