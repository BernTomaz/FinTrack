# Fluxo de autenticação

## Cadastro

```text
Nome + e-mail + senha
  -> validar e-mail único
  -> salvar senha com hash
  -> criar usuário
```

## Login

```text
E-mail + senha
  -> validar credenciais
  -> gerar JWT
  -> frontend salva token
  -> chamadas privadas enviam token
```

