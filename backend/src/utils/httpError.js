/**
 * Simple HTTP error that carries status code and public message.
 */
class HttpError extends Error {
  /**
   * @param {number} status
   * @param {string} message
   * @param {unknown} [details]
   */
  constructor(status, message, details) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
    this.details = details;
  }
}

module.exports = { HttpError };


