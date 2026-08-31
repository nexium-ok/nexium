import Download from "../components/download"
import getFlag from "../lib/getFlag";

const DownloadPage = () => {
    if (!getFlag('downloadPageEnabled', true)) return null;
    return <Download></Download>
}

export default DownloadPage;