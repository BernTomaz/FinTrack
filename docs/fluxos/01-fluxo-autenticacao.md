# Fluxo de Autenticação

## Cadastro

```text
Nome + email + senha
  -> validar email único
  -> salvar senha com hash
  -> criar usuário
```

## Login

```text
Email + senha
  -> validar credenciais
  -> gerar JWT
  -> frontend salva token
  -> chamadas privadas enviam token
```

