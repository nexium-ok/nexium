import React, { useEffect, useRef, useState } from "react";
import { getCreatedItems, uploadBadgePass } from "../../../../services/develop";
import AuthenticationStore from "../../../../stores/authentication";
import AssetList from "../assetList";
import ActionButton from "../../../actionButton";

const Passes = () => {
  const auth = AuthenticationStore.useContainer();
  const [passes, setPasses] = useState(null);
  const [selectedPlaceId, setSelectedPlaceId] = useState(null);
  const [passName, setPassName] = useState('');
  const [passDescription, setPassDescription] = useState('');
  const [feedback, setFeedback] = useState(null);
  const [locked, setLocked] = useState(false);

  const fileRef = useRef(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const placeId = params.get('selectedplaceId') || params.get('selectedPlaceId');
    if (placeId) setSelectedPlaceId(placeId);
  }, []);

  useEffect(() => {
    if (!auth.userId) return;
    getCreatedItems({ limit: 100, cursor: '', assetType: 34 })
      .then(data => setPasses(data))
      .catch(err => console.error(err));
  }, [auth.userId]);

  const onSubmit = async () => {
    if (locked) return;
    if (!fileRef.current.files.length) return setFeedback("You must select a file.");
    if (!passName) return setFeedback("You must provide a pass name.");
    if (!passDescription) return setFeedback("You must provide a description.");

    setFeedback(null);
    setLocked(true);

    try {
      await uploadBadgePass({
        name: passName,
        description: passDescription,
        assetTypeId: 34,
        placeId: selectedPlaceId,
        file: fileRef.current.files[0]
      });
      window.location.href = "/develop?View=34";
    } catch (err) {
      setFeedback(err.message);
      setLocked(false);
    }
  };

  if (selectedPlaceId) {
    return (
      <div>
        <h2>Upload a Game Pass</h2>
        <div style={{ marginBottom: '10px' }}>
          <label>Image:</label><br/>
          <input type="file" ref={fileRef} accept="image/*" style={{ width: '300px', height: '30px' }} />
          {feedback && <span style={{ color: 'red', marginLeft: '8px' }}>{feedback}</span>}
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label>Name:</label><br/>
          <input
            type="text"
            value={passName}
            onChange={e => setPassName(e.target.value)}
            style={{ width: '300px', height: '30px', borderRadius: 0, border: '1px solid #ccc' }}
          />
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label>Description:</label><br/>
          <textarea
            value={passDescription}
            onChange={e => setPassDescription(e.target.value)}
            style={{ width: '300px', height: '60px', borderRadius: 0, border: '1px solid #ccc' }}
          />
        </div>
        <ActionButton disabled={locked} label="Upload" onClick={onSubmit} />
      </div>
    );
  }

  return (
    <div>
      <h2>Your Game Passes</h2>
      {passes ? (
        passes.data.length === 0
          ? <p>You haven't created any game passes yet.</p>
          : <AssetList assets={passes.data} />
      ) : <p>Loading...</p>}
    </div>
  );
};

export default Passes;