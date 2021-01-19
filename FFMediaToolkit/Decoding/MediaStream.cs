﻿namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// A base for streams of any kind of media.
    /// </summary>
    public class MediaStream : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream"/> class.
        /// </summary>
        /// <param name="stream">The associated codec.</param>
        /// <param name="options">Extra options.</param>
        internal MediaStream(Decoder stream, MediaOptions options)
        {
            Stream = stream;
            Options = options;

            Threshold = TimeSpan.FromSeconds(0.5).ToTimestamp(Info.TimeBase);
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public StreamInfo Info => Stream.Info;

        /// <summary>
        /// Gets the timestamp of the recently decoded frame in the media stream.
        /// </summary>
        public TimeSpan Position => Math.Max(Stream.RecentlyDecodedFrame.PresentationTimestamp, 0).ToTimeSpan(Info.TimeBase);

        /// <summary>
        /// Indicates whether the stream has buffered frame data.
        /// </summary>
        public bool IsBufferEmpty => Stream.IsBufferEmpty;

        /// <summary>
        /// Gets the options configured for this <see cref="MediaStream"/>.
        /// </summary>
        protected MediaOptions Options { get; }

        private Decoder Stream { get; }

        private long Threshold { get; }

        /// <summary>
        /// Discards all buffered frame data associated with this stream.
        /// </summary>
        public void DiscardBufferedData() => Stream.DiscardBufferedData();

        /// <summary>
        /// Seeks the stream to the specified timestamp.
        /// </summary>
        /// <param name="ts">The timestamp.</param>
        public void SeekTo(long ts)
        {
            DiscardBufferedData();

            var currentTs = Stream.RecentlyDecodedFrame.PresentationTimestamp;
            if (ts < currentTs || ts >= currentTs + Threshold)
            {
                Stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            Stream.SkipFrames(ts);
        }

        /// <summary>
        /// Seeks the stream to the specified position.
        /// </summary>
        /// <param name="time">The desired target position.</param>
        public void SeekTo(TimeSpan time)
        {
            SeekTo(time.ToTimestamp(Info.TimeBase));
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            DiscardBufferedData();
            Stream.Dispose();
        }

        /// <summary>
        /// Gets the data belonging to the next frame in the stream.
        /// </summary>
        /// <returns>The next frame's data.</returns>
        internal MediaFrame GetNextFrame() => Stream.GetNextFrame();

        /// <summary>
        /// Seeks the stream to the specified time and returns the nearest frame's data.
        /// </summary>
        /// <param name="time">A specific point in time in this stream.</param>
        /// <returns>The nearest frame's data.</returns>
        internal MediaFrame GetFrame(TimeSpan time)
        {
            var ts = time.ToTimestamp(Info.TimeBase);
            var frame = GetFrameByTimestamp(ts);
            return frame;
        }

        private MediaFrame GetFrameByTimestamp(long ts)
        {
            var frame = Stream.RecentlyDecodedFrame;
            ts = Math.Max(0, Math.Min(ts, Info.DurationRaw));

            if (ts > frame.PresentationTimestamp && ts < frame.PresentationTimestamp + Threshold)
            {
                return Stream.GetNextFrame();
            }
            else if (ts != frame.PresentationTimestamp)
            {
                SeekTo(ts);
            }

            return Stream.RecentlyDecodedFrame;
        }
    }
}
