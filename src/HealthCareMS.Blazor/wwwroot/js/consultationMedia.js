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
};
