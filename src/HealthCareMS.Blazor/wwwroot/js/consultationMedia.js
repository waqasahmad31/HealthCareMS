window.HealthCareMSMedia = {
  streams: {},
  async startPreview(videoElementId) {
    const video = document.getElementById(videoElementId);
    if (!video || !navigator.mediaDevices?.getUserMedia) {
      return { success: false, message: "Camera or microphone is unavailable in this browser." };
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
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
