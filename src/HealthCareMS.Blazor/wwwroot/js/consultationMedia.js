window.HealthCareMSMedia = {
  streams: {},
  async startPreview(videoElementId, options) {
    const video = document.getElementById(videoElementId);
    if (!video || !navigator.mediaDevices?.getUserMedia) {
      return { success: false, message: "Camera or microphone is unavailable in this browser." };
    }

    try {
      const lowBandwidth = Boolean(options?.lowBandwidth || navigator.connection?.saveData);
      const constraints = lowBandwidth
        ? {
            video: {
              width: { ideal: 320, max: 480 },
              height: { ideal: 180, max: 270 },
              frameRate: { ideal: 10, max: 15 },
            },
            audio: { echoCancellation: true, noiseSuppression: true },
          }
        : {
            video: {
              width: { ideal: 640, max: 960 },
              height: { ideal: 360, max: 540 },
              frameRate: { ideal: 24, max: 30 },
            },
            audio: { echoCancellation: true, noiseSuppression: true },
          };
      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      video.srcObject = stream;
      video.muted = true;
      await video.play();
      this.streams[videoElementId] = stream;
      return {
        success: true,
        message: "Camera and microphone are ready.",
        videoTracks: stream.getVideoTracks().length,
        audioTracks: stream.getAudioTracks().length,
      };
    } catch (error) {
      return { success: false, message: error?.message || "Permission was denied." };
    }
  },
  stopPreview(videoElementId) {
    const stream = this.streams[videoElementId];
    if (stream) {
      stream.getTracks().forEach((track) => track.stop());
      delete this.streams[videoElementId];
    }

    const video = document.getElementById(videoElementId);
    if (video) {
      video.srcObject = null;
    }
  },
  downloadFile(fileName, contentType, base64Content) {
    const link = document.createElement("a");
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    for (let index = 0; index < byteCharacters.length; index += 1) {
      byteNumbers[index] = byteCharacters.charCodeAt(index);
    }

    const blob = new Blob([new Uint8Array(byteNumbers)], { type: contentType });
    link.href = URL.createObjectURL(blob);
    link.download = fileName || "Attachment";
    document.body.appendChild(link);
    link.click();
    URL.revokeObjectURL(link.href);
    link.remove();
  },
};
