# SSL Certificates

## For Development

Generate self-signed certificates:

```bash
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout key.pem -out cert.pem \
  -subj "/C=NO/ST=Oslo/L=Oslo/O=TeleDoctor/CN=localhost"
```

## For Production

Use proper SSL certificates from:
- Let's Encrypt (free, automated)
- Azure Key Vault
- Commercial CA

Place your certificates here:
- `cert.pem` - SSL certificate
- `key.pem` - Private key

**Important**: Never commit real certificates to version control!
